#include <p18cxxx.h>
#include <usart.h>
#include "system\typedefs.h"

#include "system\usb\usb.h"

#include "io_cfg.h"             // I/O pin mapping
#include "user\user.h"

#pragma udata

byte counter;

DATA_PACKET dataPacket;

// Timer0 - 1 second interval setup.
// Fosc/4 = 12MHz
// Use /256 prescalar, this brings counter freq down to 46,875 Hz
// Timer0 should = 65536 - 46875 = 18661 or 0x48E5
#define TIMER0L_VAL         0xE5
#define TIMER0H_VAL         0x48

void ServiceRequests(void);
byte ReadPOT(void);
void Blink(byte);

#pragma code

void ProcessIO(void)
{   
    // User Application USB tasks
    if((usb_device_state < CONFIGURED_STATE)||(UCONbits.SUSPND==1)) return;
    
    //UCAM
    ServiceRequests();
}

void ServiceRequests(void)
{    
    if(USBGenRead((byte*)&dataPacket,sizeof(dataPacket)))
    {   
        counter = 0;
        switch(dataPacket.CMD)
        {
            case SET_LED_COMMAND: //[0xEE, Onstate]
                Blink(dataPacket._byte[1]);
                counter=0x02; //sends back same command
                break;
           case GET_ADC_COMMAND: //[0xED. 8-bit data]
                dataPacket._byte[1] = ReadPOT();
                counter=0x02; //returns[0xED, command]
                break;
            case RESET:
                Reset();
                break;
                
            default:
                break;
        }
        if(counter != 0)
        {
            if(!mUSBGenTxIsBusy())
                USBGenWrite((byte*)&dataPacket,counter);
        }
    }
    return;
}

void Blink(byte onState)
{
    LATD = onState;
    return;
}

byte ReadPOT(void)
{
    byte low, high;

    ADCON0bits.GO = 1;              // Start AD conversion
    while(ADCON0bits.NOT_DONE);     // Wait for conversion
    low = ADRESL;
    high = ADRESH;
    low >>= 2;
    high <<= 6;

    return (low | high);
}

void UserInit(void)
{
    mInitAllLEDs();
    TRISAbits.TRISA0=1;
    ADCON0=0x01;
    ADCON2=0x3C;
    ADCON2bits.ADFM = 1;   // ADC result right justified
    TRISD = 0x18;
}
