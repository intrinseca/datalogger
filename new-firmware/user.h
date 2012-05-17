#ifndef PICDEM_FS_DEMO_H
#define PICDEM_FS_DEMO_H

/** I N C L U D E S **********************************************************/
#include "GenericTypeDefs.h"
#include "usb_config.h"

extern volatile unsigned char usbgen_out[USBGEN_EP_SIZE];
extern volatile unsigned char usbgen_in[USBGEN_EP_SIZE];

/** D E F I N I T I O N S ****************************************************/
/* PICDEM FS USB Demo Version */
#define MINOR_VERSION   0x00    //Demo Version 1.00
#define MAJOR_VERSION   0x01

/* Temperature Mode */
#define TEMP_REAL_TIME  0x00
#define TEMP_LOGGING    0x01

typedef enum
{
    RD_POT          = 0xED,
    SET_PORTA       = 0xEE,
    RESET           = 0xFF
}TYPE_CMD;

/** S T R U C T U R E S ******************************************************/
typedef union DATA_PACKET
{
    BYTE _byte[USBGEN_EP_SIZE];  //For byte access
    WORD _word[USBGEN_EP_SIZE/2];//For word access(USBGEN_EP_SIZE msut be even)
    struct
    {
        BYTE CMD;
        BYTE len;
    };
    struct
    {
        unsigned :8;
        BYTE ID;
    };
    struct
    {
        unsigned :8;
        BYTE led_num;
        BYTE led_status;
    };
    struct
    {
        unsigned :8;
        WORD word_data;
    };
} DATA_PACKET;

/** P U B L I C  P R O T O T Y P E S *****************************************/
void UserInit(void);
void ProcessIO(void);

#endif //PICDEM_FS_DEMO_H
