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

def fact(n)
    if (n == 0)
        return 1
    else 
        return n * fact(n-1)
    end
end   

puts 'factorial: '
puts fact(gets())

array = []
array[0] = 10
array[1] = 9
array[2] = 8

def bubbleSort(a)
    size = len(a)
    for(i = 0; i<size; i+=1)
        for(j = size-1; j>i; j-=1)
            if(a[j-1] > a[j])
                x=a[j-1]
                a[j-1]=a[j]
                a[j]=x
            end
        end
    end
    return a
end

array = bubbleSort(array)

puts 'sort array: '
puts array[0]
puts array[1]
puts array[2]

printHello()