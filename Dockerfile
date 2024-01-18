FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022

#Install chocolatey
RUN powershell -Command Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

#Install packages
RUN choco install git python nodejs -y

# Install certs for python
RUN pip install certifi pip-system-certs && \
    certutil -generateSSTFromWU roots.sst && certutil -addstore -f root roots.sst

# Install wasi-sdk
RUN mkdir C:\tools\wasi-sdk && \
    cd c:\tools\wasi-sdk && \
    curl -OL https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-20/wasi-sdk-20.0.m-mingw.tar.gz && \
    tar -xf wasi-sdk-20.0.m-mingw.tar.gz -C . --strip-components=1 && \
    setx PATH "%PATH%;C:\\tools\\wasi-sdk\\wasi-sdk-20.0+m\\bin"

# Install emsdk
RUN mkdir C:\tools\emsdk && \
    cd c:\tools\emsdk && \
    curl -OL https://github.com/emscripten-core/emsdk/archive/refs/tags/3.1.23.zip && \
    tar -xf 3.1.23.zip -C . --strip-components=1 && \
    setx PATH "%PATH%;C:\\tools\\emsdk"

# Copy current project to container
COPY . C:\\build

# Setup and init build env by building sample project once (this downloads additional tooling)
RUN cd c:\build && \
    npm install wasm-opt -g && \
    emsdk install 3.1.23 && \
    emsdk activate 3.1.23 && \
    dotnet build samples\SaturdayNightLotto

CMD [ "cmd" ]