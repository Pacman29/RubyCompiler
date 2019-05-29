# BUILD EXECUTE FILE
1.  Run run_container script (you need  docker)
2.  Into container:
    1.   Run `llc -filetype=obj file.bc`
    2.   Run `gcc file.o`
    3.   Exec `./file.out`
