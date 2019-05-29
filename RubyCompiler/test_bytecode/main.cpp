#include <stdio.h>
extern "C" int num(int);

int main() {
    printf("num: %d",num(1));
    return 0;
}