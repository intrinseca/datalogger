#include "usb.h"
#include "USB/usb_function_generic.h"
#include "user.h"

extern void _startup (void);        // See c018i.c in your C18 compiler dir
#pragma code _RESET_INTERRUPT_VECTOR = 0x000800
void _reset (void)
{
    _asm goto _startup _endasm
}
#pragma code

#pragma code _HIGH_INTERRUPT_VECTOR = 0x000808
void _high_ISR (void)
{
    ;
}

#pragma code _LOW_INTERRUPT_VECTOR = 0x000818
void _low_ISR (void)
{
    ;
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
    InitializeSystem();

    while(1)
    {
        USBDeviceTasks();
        ProcessIO();
    }
}

static void InitializeSystem()
{
    ADCON1 |= 0x0F;
    UserInit();
    USBDeviceInit();
}

void USBCBInitEP(void)
{
    USBEnableEndpoint(USBGEN_EP_NUM, USB_OUT_ENABLED|USB_IN_ENABLED|USB_HANDSHAKE_ENABLED|USB_DISALLOW_SETUP);
    USBEnableEndpoint(2, USB_IN_ENABLED|USB_HANDSHAKE_ENABLED|USB_DISALLOW_SETUP);
    
    USBGenericOutHandle = USBGenRead(USBGEN_EP_NUM, (BYTE*)&OUTPacket, USBGEN_EP_SIZE);
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
