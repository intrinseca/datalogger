#ifndef USER_H
#define USER_H

#include "GenericTypeDefs.h"
#include "usb_config.h"

extern volatile unsigned char usbgen_out[USBGEN_EP_SIZE];
extern volatile unsigned char usbgen_in[USBGEN_EP_SIZE];

//Commands from PC to device
typedef enum
{
    ADC_READ        = 0x10,
    PORTD_SET       = 0x20,
    SAMPLING_START  = 0x30,
    SAMPLING_STOP   = 0x31,
    SAMPLING_SEND   = 0x32
}TYPE_CMD;

//Used to access fields of the data packet being sent
typedef union DATA_PACKET
{
    BYTE _byte[USBGEN_EP_SIZE];  //For byte access
    WORD _word[USBGEN_EP_SIZE/2];//For word access(USBGEN_EP_SIZE msut be even)
    struct
    {
        BYTE CMD;
        BYTE len;
    };
} DATA_PACKET;

void UserInit(void);
void ProcessIO(void);

#endif USER_H
