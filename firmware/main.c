#include "usb.h"
#include "USB/usb_function_generic.h"
#include "user.h"
#include "timer.h"
#include "isr.h"

extern void _startup (void);        // See c018i.c in your C18 compiler dir
#pragma code _RESET_INTERRUPT_VECTOR = 0x000800
void _reset (void)
{
    _asm goto _startup _endasm
}
#pragma code

extern USB_HANDLE USBGenericOutHandle;
extern USB_HANDLE USBGenericInHandle;
extern DATA_PACKET INPacket;
extern DATA_PACKET OUTPacket;

static void InitializeSystem(void);
void USBDeviceTasks(void);

void main()
{
    unsigned char state = 0;

    InitializeSystem();

    LATDbits.LATD0 = 0;
    TRISDbits.TRISD0 = 0;

    while(1)
    {
        // disable interrupts while checking
        INTCONbits.GIEH = 0;    // disable all interrupts
        INTCONbits.GIEL = 0;

        if(watchdog_cntr == 0) {
            if(state == 0) {
                watchdog_cntr = 50;
                LATDbits.LATD0 = 1;
                state = 1;
            } else {
                watchdog_cntr = 950;
                LATDbits.LATD0 = 0;
                state = 0;
            }
        }

        INTCONbits.GIEH = 1;    // re-enable all interrupts
        INTCONbits.GIEL = 1;
//        USBDeviceTasks();
//        ProcessIO();
    }
}

static void InitializeSystem()
{
    isr_init();
    timer_init();
    //ADCON1 |= 0x0F;
    //UserInit();
    //USBDeviceInit();
}

void USBCBInitEP(void)
{
    USBEnableEndpoint(USBGEN_EP_NUM,USB_OUT_ENABLED|USB_IN_ENABLED|USB_HANDSHAKE_ENABLED|USB_DISALLOW_SETUP);
    USBGenericOutHandle = USBGenRead(USBGEN_EP_NUM,(BYTE*)&OUTPacket,USBGEN_EP_SIZE);
}

BOOL USER_USB_CALLBACK_EVENT_HANDLER(USB_EVENT event, void *pdata, WORD size)
{
    switch((INT)event)
    {
        case EVENT_TRANSFER:
            //Add application specific callback task or callback function here if desired.
            break;
        case EVENT_CONFIGURED:
            USBCBInitEP();
            break;
        default:
            break;
    }
    return TRUE;
}
