#include "usb.h"
#include "USB/usb_function_generic.h"

#include "HardwareProfile.h"
#include "user.h"
#include "main.h"
#include "timer.h"

#pragma udata

BYTE counter;

#pragma udata USB_VARIABLES=0x500

DATA_PACKET INCommand;
DATA_PACKET OUTCommand;
DATA_PACKET INData;

#pragma udata

USB_HANDLE USBCommandOutHandle;
USB_HANDLE USBCommandInHandle;
USB_HANDLE USBDataInHandle;

BYTE ReadPOT(void);
void ServiceRequests(void);

#pragma code

void UserInit(void)
{
    mInitAllLEDs();
    mInitAllSwitches();

    //mInitPOT();

    USBCommandInHandle = 0;
    USBCommandOutHandle = 0;
    USBDataInHandle = 0;
}

//Called from the main loop to process user tasks
//There's nothing here that couldn't really be in the main loop itself.
void ProcessIO(void)
{   
    //Skip if the usb is not active
    if((USBDeviceState < CONFIGURED_STATE)||(USBSuspendControl==1)) return;

    //Process USB commands
    ServiceRequests();
}

//Run the ADC conversion and return the higher 8 bits of the result
BYTE ReadPOT(void)
{
    BYTE low, high;

    //Configure ADC peripheral
    TRISAbits.TRISA0=1;
    ADCON0=0x01;
    ADCON2=0x3C;
    ADCON2bits.ADFM = 1;

    //Trigger conversion
    ADCON0bits.GO = 1;
    //Wait for complete
    while(ADCON0bits.NOT_DONE);

    //Drop lower two bits and return
    low = ADRESL;
    high = ADRESH;
    low >>= 2;
    high <<= 6;

    return (low | high);
}

//Process usb commands
void ServiceRequests(void)
{    
    //Check to see if data has arrived
    if(!USBHandleBusy(USBCommandOutHandle))
    {
        //Length of the response packet
        counter = 1;

        //Pre-fill response based on command received
        INCommand.CMD=OUTCommand.CMD;

        //process the command
        switch(OUTCommand.CMD)
        {
            case PORTD_SET:
                //Set PORTD to the value in the first data byte
                LATD = OUTCommand._byte[1];

                //Response is the echo of the request
                INCommand._byte[1] = OUTCommand._byte[1];
                counter = 0x01;
                break;

            //Return the current value of the ADC in the first data byte
            case ADC_READ:
                INCommand._byte[1] = ReadPOT();
                counter = 0x02;
                break;

            case SAMPLING_START:
                datalogger_state = CAPTURING;
                break;

            case SAMPLING_STOP:
                datalogger_state = NOT_CAPTURING;
                break;

            //TODO: Is this here for any good reason?
            default:
                Nop();
                break;
        }

        //If there was some response data, send it back
        if(counter != 0)
        {
            if(!USBHandleBusy(USBCommandInHandle))
            {
                USBCommandInHandle = USBGenWrite(COMMAND_EP, (BYTE*)&INCommand, counter);
            }
        }
        
        //Re-arm the OUT endpoint for the next packet
        USBCommandOutHandle = USBGenRead(COMMAND_EP, (BYTE*)&OUTCommand, EP_SIZE);
    }
}

void sendSamples(void)
{
    BYTE low, high;
    unsigned char i;

    isr_disable_interrupts();

    if(iso_test_cntr == 0)
    {
        if(!USBHandleBusy(USBDataInHandle))
        {
            low = ADRESL;
            high = ADRESH;
            low >>= 2;
            high <<= 6;

            for(i = 0; i < 64; i++)
            {
                INData._byte[i] = (low | high);
            }

            USBDataInHandle = USBGenWrite(DATA_EP, (BYTE*)&INData, 2);
        }

        iso_test_cntr = 2;
    }

    isr_enable_interrupts();
}