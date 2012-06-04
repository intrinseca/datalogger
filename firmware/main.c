#include "main.h"
#include "usb.h"
#include "USB/usb_function_generic.h"
#include "comms.h"
#include "timer.h"
#include "isr.h"
#include "watchdog.h"
#include "adc.h"

extern void _startup (void);        // See c018i.c in your C18 compiler dir
#pragma code _RESET_INTERRUPT_VECTOR = 0x000800
void _reset (void)
{
    _asm goto _startup _endasm
}
#pragma code

unsigned char datalogger_state;

extern USB_HANDLE USBCommandOutHandle;
extern USB_HANDLE USBCommandInHandle;
extern USB_HANDLE USBDataInHandle;
extern DATA_PACKET INCommand;
extern DATA_PACKET OUTCommand;
extern DATA_PACKET INData;

static void InitializeSystem(void);

void main()
{
    InitializeSystem();
    USBDeviceAttach();

    datalogger_state = NOT_CAPTURING;
    
    while(1)
    {
        watchdog_tick();

        if(USBDeviceState == CONFIGURED_STATE && USBSuspendControl != 1)
        {
            //Process USB commands
            comms_process_command();
        }

        switch (datalogger_state) {
            case NOT_CAPTURING:
                break;
            case CAPTURING:
                comms_send_samples();
                break;
            default:
                break;
        }
    }
}

static void InitializeSystem()
{
    isr_init();
    timer_init();
    watchdog_init();
    adc_init();

    //ADCON1 |= 0x0F;
    comms_init();
    USBDeviceInit();
}

void USBCBInitEP(void)
{
    USBEnableEndpoint(COMMAND_EP, USB_OUT_ENABLED|USB_IN_ENABLED|USB_HANDSHAKE_ENABLED|USB_DISALLOW_SETUP);
    USBEnableEndpoint(DATA_EP, USB_IN_ENABLED|USB_HANDSHAKE_ENABLED|USB_DISALLOW_SETUP);

    USBCommandOutHandle = USBGenRead(COMMAND_EP, (BYTE*)&OUTCommand, EP_SIZE);
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
