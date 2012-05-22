#include "usb.h"
#include "USB/usb_function_generic.h"

#include "HardwareProfile.h"
#include "user.h"

#pragma udata

BYTE counter;

#pragma udata USB_VARIABLES=0x500

DATA_PACKET INPacket;
DATA_PACKET OUTPacket;

#pragma udata

USB_HANDLE USBGenericOutHandle;
USB_HANDLE USBGenericInHandle;

/** P R I V A T E  P R O T O T Y P E S ***************************************/

BYTE ReadPOT(void);
void ServiceRequests(void);

/** D E C L A R A T I O N S **************************************************/
#if defined(__18CXX)
    #pragma code
#endif
void UserInit(void)
{
    mInitAllLEDs();
    mInitAllSwitches();

    mInitPOT();

    USBGenericInHandle = 0;
    USBGenericOutHandle = 0;
}//end UserInit


/******************************************************************************
 * Function:        void ProcessIO(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        This function is a place holder for other user routines.
 *                  It is a mixture of both USB and non-USB tasks.
 *
 * Note:            None
 *****************************************************************************/
void ProcessIO(void)
{   
    // User Application USB tasks
    if((USBDeviceState < CONFIGURED_STATE)||(USBSuspendControl==1)) return;
		
    ServiceRequests();
}

BYTE ReadPOT(void)
{
    BYTE low, high;

    ADCON0bits.GO = 1;              // Start AD conversion
    while(ADCON0bits.NOT_DONE);     // Wait for conversion
    low = ADRESL;
    high = ADRESH;
    low >>= 2;
    high <<= 6;

    return (low | high);
}

void ServiceRequests(void)
{    
    //Check to see if data has arrived
    if(!USBHandleBusy(USBGenericOutHandle))
    {   
        counter = 1;

        INPacket.CMD=OUTPacket.CMD;
        INPacket.len=OUTPacket.len;

        //process the command
        switch(OUTPacket.CMD)
        {
            case SET_PORTA:
                LATD = INPacket._byte[1];
                counter=0x02;
                break;
            
            case RD_POT:
                mInitPOT();

                INPacket._byte[1] = ReadPOT();
                counter=0x02;
                break;
                
            case RESET:
                Reset();
                break;
                
            default:
                Nop();
                break;
        }

        if(counter != 0)
        {
            if(!USBHandleBusy(USBGenericInHandle))
            {
                USBGenericInHandle = USBGenWrite(USBGEN_EP_NUM, (BYTE*)&INPacket, counter);
            }
        }
        
        //Re-arm the OUT endpoint for the next packet
        USBGenericOutHandle = USBGenRead(USBGEN_EP_NUM, (BYTE*)&OUTPacket, USBGEN_EP_SIZE);
    }
}