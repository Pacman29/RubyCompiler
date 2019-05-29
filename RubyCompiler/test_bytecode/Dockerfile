FROM ubuntu:18.04
	
##
## BUILD LLVM
##
RUN echo "BUILD LLVM"
RUN apt-get update && apt-get install -y \
  xz-utils \
   build-essential \
   curl \
   && rm -rf /var/lib/apt/lists/* \
   && curl -SL http://releases.llvm.org/8.0.0/clang+llvm-8.0.0-x86_64-linux-gnu-ubuntu-18.04.tar.xz \
   | tar -xJC . && \
   mv clang+llvm-8.0.0-x86_64-linux-gnu-ubuntu-18.04 clang_8.0.0 && \
   echo 'export PATH=/clang_8.0.0/bin:$PATH' >> ~/.bashrc && \
   echo 'export LD_LIBRARY_PATH=/clang_8.0.0/lib:LD_LIBRARY_PATH' >> ~/.bashrc

WORKDIR /home/test
CMD [ "/bin/bash" ]