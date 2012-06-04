#ifndef USER_H
#define USER_H

#include "GenericTypeDefs.h"
#include "usb_config.h"

extern volatile unsigned char usbgen_out[EP_SIZE];
extern volatile unsigned char usbgen_in[EP_SIZE];

//Commands from PC to device
typedef enum
{
    ADC_READ        = 0x10,
    PORTD_SET       = 0x20,
    CAPTURE_START   = 0x30,
    CAPTURE_STOP    = 0x31
} TYPE_CMD;

//Used to access fields of the data packet being sent
typedef union DATA_PACKET
{
    BYTE _byte[EP_SIZE];  //For byte access
    WORD _word[EP_SIZE/2];//For word access(USBGEN_EP_SIZE msut be even)
    struct
    {
        BYTE CMD;
        BYTE len;
    };
} DATA_PACKET;

extern USB_HANDLE USBCommandOutHandle;
extern USB_HANDLE USBCommandInHandle;
extern USB_HANDLE USBDataInHandle;
extern DATA_PACKET * INCommand;
extern DATA_PACKET * OUTCommand;
extern DATA_PACKET * INData;

void comms_init(void);
void comms_process_command(void);
void comms_send_samples(void);
#endif USER_H
