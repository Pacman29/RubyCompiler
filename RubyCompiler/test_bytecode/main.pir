.sub main

$P1 = new "Integer"
$P1 = sample()

puts($P1)

.end

.include "stdlib/stdlib.pir"

.sub sample



.local pmc a
a= new "Integer"
a = 42

.local pmc b
b= new "Integer"
$P0 = new "Integer"
$P0 = a
$P1 = new "Integer"
$P1 = $P0 / 2
b = $P1
$P0 = new "Integer"
$P0 = b
.return($P0)

.end
