#include "usb.h"
#include "USB/usb_function_generic.h"

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

static void InitializeSystem(void);
void USBDeviceTasks(void);

void main()
{
    InitializeSystem();

    while(1)
    {
        USBDeviceTasks();
    }
}

static void InitializeSystem()
{
    ADCON1 |= 0x0F;
    USBDeviceInit();
}

BOOL USER_USB_CALLBACK_EVENT_HANDLER(USB_EVENT event, void *pdata, WORD size)
{
    ;
}