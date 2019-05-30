.sub main

puts("input: ")

.local pmc n
n= new "Integer"

$P0 = new "Integer"
$P0 = gets()
n = $P0

.local pmc j
j= new "Integer"
j = 0
label_6:
$P8 = new "Integer"
$P8 = j
$P9 = new "Integer"
$P9 = n
$I2 = islt $P8, $P9

unless $I2 goto label_7
$P10 = new "Integer"
$P10 = j
$P11 = new "Integer"
$P11 = j

$P12 = new "Integer"
$P12 = func($P10,$P11)

puts($P12)
j += 1

goto label_6
label_7:

.local pmc arr
arr = new "ResizablePMCArray"
arr[0] = 0
arr[1] = "this is 1"
arr[2] = 3.64
$P13 = new "Integer"
$P13 = arr

$P14 = new "Integer"
$P14 = len($P13)

puts($P14)

.local pmc i
i= new "Integer"
i = 0

label_8:
$P15 = new "Integer"
$P15 = i
$P16 = new "Integer"
$P16 = arr

$P17 = new "Integer"
$P17 = len($P16)
$I3 = islt $P15, $P17

unless $I3 goto label_9
$P18 = new "Integer"
$P18 = i
$P19 = new "Integer"
$P19 = arr[$P18]

puts($P19)

i += 1

goto label_8
label_9:

puts("factorial: ")

$P27 = new "Integer"
$P27 = gets()

$P28 = new "Integer"
$P28 = fact($P27)

puts($P28)

.local pmc array
array = new "ResizablePMCArray"
array[0] = 10
array[1] = 9
array[2] = 8
$P7 = new "Integer"
$P7 = array

$P8 = new "Integer"
$P8 = bubbleSort($P7)
array = $P8

puts("sort array: ")
$P0 = new "Integer"
$P0 = array[0]

puts($P0)
$P1 = new "Integer"
$P1 = array[1]

puts($P1)
$P2 = new "Integer"
$P2 = array[2]

puts($P2)

printHello()

.end

.include "stdlib/stdlib.pir"

.sub func

.param pmc a
.param pmc b

$P0 = new "Integer"
$P0 = a
$P1 = new "Integer"
$P1 = b
$P2 = new "Integer"
$P2 = $P0 * $P1
.return($P2)

.end

.sub func

.param pmc a
.param pmc b

$P0 = new "Integer"
$P0 = a
$P1 = new "Integer"
$P1 = b
$P2 = new "Integer"
$P2 = $P0 * $P1
.return($P2)

.end

.sub fact

.param pmc n


$P20 = new "Integer"
$P20 = n
$P21 = new "Integer"
$P21 = 0
$I4 = iseq $P20, $P21

if $I4 goto label_10
goto label_11
label_10:
.return(1)

goto label_12
label_11:
$P22 = new "Integer"
$P22 = n
$P23 = new "Integer"
$P23 = n
$P24 = new "Integer"
$P24 = $P23 - 1

$P25 = new "Integer"
$P25 = fact($P24)
$P26 = new "Integer"
$P26 = $P22 * $P25
.return($P26)

label_12:

.end

.sub fact

.param pmc n


$P20 = new "Integer"
$P20 = n
$P21 = new "Integer"
$P21 = 0
$I4 = iseq $P20, $P21

if $I4 goto label_10
goto label_11
label_10:
.return(1)

goto label_12
label_11:
$P22 = new "Integer"
$P22 = n
$P23 = new "Integer"
$P23 = n
$P24 = new "Integer"
$P24 = $P23 - 1

$P25 = new "Integer"
$P25 = fact($P24)
$P26 = new "Integer"
$P26 = $P22 * $P25
.return($P26)

label_12:

.end

.sub bubbleSort

.param pmc a


.local pmc size
size= new "Integer"
$P0 = new "Integer"
$P0 = a

$P1 = new "Integer"
$P1 = len($P0)
size = $P1

.local pmc i
i= new "Integer"
i = 0

label_13:
$P0 = new "Integer"
$P0 = i
$P1 = new "Integer"
$P1 = size
$I5 = islt $P0, $P1

unless $I5 goto label_14

.local pmc j
j= new "Integer"
$P0 = new "Integer"
$P0 = size
$P1 = new "Integer"
$P1 = $P0 - 1
j = $P1

label_15:
$P0 = new "Integer"
$P0 = j
$P1 = new "Integer"
$P1 = i
$I6 = isgt $P0, $P1

unless $I6 goto label_16

$P2 = new "Integer"
$P2 = j
$P3 = new "Integer"
$P3 = $P2 - 1
$P4 = new "Integer"
$P4 = a[$P3]
$P5 = new "Integer"
$P5 = j
$P6 = new "Integer"
$P6 = a[$P5]
$I7 = isgt $P4, $P6

if $I7 goto label_17
goto label_18
label_17:

.local pmc x
x= new "Integer"
$P0 = new "Integer"
$P0 = j
$P1 = new "Integer"
$P1 = $P0 - 1
$P2 = new "Integer"
$P2 = a[$P1]
x = $P2
$P0 = new "Integer"
$P0 = j
$P1 = new "Integer"
$P1 = $P0 - 1
$P2 = new "Integer"
$P2 = j
$P3 = new "Integer"
$P3 = a[$P2]
a[$P1] = $P3
$P4 = new "Integer"
$P4 = j
$P5 = new "Integer"
$P5 = x
a[$P4] = $P5

label_18:

j -= 1

goto label_15
label_16:

i += 1

goto label_13
label_14:
$P6 = new "Integer"
$P6 = a
.return($P6)

.end

.sub printHello



.local pmc i
i= new "Integer"
i = 0

label_1:
$P3 = new "Integer"
$P3 = i

$P4 = new "Integer"
$P4 = func(5,5)
$I0 = islt $P3, $P4

unless $I0 goto label_2

$P5 = new "Integer"
$P5 = i
$P6 = new "Integer"
$P6 = $P5 % 2
$P7 = new "Integer"
$P7 = 0
$I1 = iseq $P6, $P7

if $I1 goto label_3
goto label_4
label_3:

puts("HELLO\n")

goto label_5
label_4:

puts("WORLD\n")

label_5:

i += 1

goto label_1
label_2:

.end
