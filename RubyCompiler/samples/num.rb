puts 'input: '
n = gets()

def func a, b
	return a*b
end 

def printHello()
    for(i = 0; i<func(5,5); i+=1)
        if( i%2 == 0)
            puts 'HELLO\n'
        else 
            puts 'WORLD\n'
        end  
    end
end

j = 0

while (j<n)
    puts func(j,j)
    j+=1
end

arr = []

arr[0] = 0
arr[1] = 'this is 1'
arr[2] = 5.64 - 2.0

puts len(arr)

for(i = 0; i<len(arr); i+=1)
    puts arr[i]
end

printHello()