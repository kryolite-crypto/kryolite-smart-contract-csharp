FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022

Run echo "Start"

#Install chocolatey
RUN powershell -Command Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

#Install packages
RUN choco install git python nodejs -y
RUN npm install wasm-opt -g

# Install certs for python
RUN pip install certifi pip-system-certs && \
    certutil -generateSSTFromWU roots.sst && certutil -addstore -f root roots.sst

# Install wasi-sdk
RUN mkdir C:\tools\wasi-sdk && \
    cd c:\tools\wasi-sdk && \
    curl -OL https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-20/wasi-sdk-20.0.m-mingw.tar.gz && \
    tar -xf wasi-sdk-20.0.m-mingw.tar.gz -C . --strip-components=1 && \
    setx WASI_SDK_PATH "C:\tools\wasi-sdk"

# Install emsdk
RUN mkdir C:\tools\emsdk && \
    cd c:\tools\emsdk && \
    curl -OL https://github.com/emscripten-core/emsdk/archive/refs/tags/3.1.23.zip && \
    tar -xf 3.1.23.zip -C . --strip-components=1 && \
    setx EMSDK "C:\tools\emsdk"

# Setup emsdk
RUN C:\tools\emsdk\emsdk install 3.1.23 && \
    C:\tools\emsdk\emsdk activate 3.1.23 --permanent

# Copy current project to container.
# We don't have published nuget packages yet,
# so this will be used as build environment
COPY . C:\\build

# Build sample project once, this will download additional tooling
WORKDIR C:\\build
RUN dotnet build samples\SaturdayNightLotto
# & dir C:\build\samples\SaturdayNightLotto\obj\Debug\net8.0\wasi-wasm & type C:\build\samples\SaturdayNightLotto\obj\Debug\net8.0\wasi-wasm\generated\Kryolite.SmartContract.Generator\Kryolite.SmartContract.Generator.ExportsGenerator\ContractExports_g.cs

CMD [ "cmd", "/C", "ping -t localhost" ]