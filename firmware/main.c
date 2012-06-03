#include "usb.h"
#include "USB/usb_function_generic.h"
#include "user.h"
#include "timer.h"
#include "isr.h"
#include "watchdog.h"

extern void _startup (void);        // See c018i.c in your C18 compiler dir
#pragma code _RESET_INTERRUPT_VECTOR = 0x000800
void _reset (void)
{
    _asm goto _startup _endasm
}
#pragma code

extern USB_HANDLE USBCommandOutHandle;
extern USB_HANDLE USBCommandInHandle;
extern DATA_PACKET INPacket;
extern DATA_PACKET OUTPacket;

static void InitializeSystem(void);

void main()
{
    InitializeSystem();
    USBDeviceAttach();

    while(1)
    {
        watchdog_tick();
        ProcessIO();
    }
}

static void InitializeSystem()
{
    isr_init();
    timer_init();
    watchdog_init();

    //ADCON1 |= 0x0F;
    UserInit();
    USBDeviceInit();
}

void USBCBInitEP(void)
{
    USBEnableEndpoint(COMMAND_EP, USB_OUT_ENABLED|USB_IN_ENABLED|USB_HANDSHAKE_ENABLED|USB_DISALLOW_SETUP);
    USBEnableEndpoint(DATA_EP, USB_IN_ENABLED|USB_HANDSHAKE_DISABLED|USB_DISALLOW_SETUP);

    USBCommandOutHandle = USBGenRead(COMMAND_EP, (BYTE*)&OUTPacket, EP_SIZE);
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
