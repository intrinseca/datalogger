#ifndef MAIN_H
#define MAIN_H

enum DATALOGGER_STATE {
    NOT_CAPTURING,
    CAPTURING
};

extern unsigned char datalogger_state;

#endif MAIN_H