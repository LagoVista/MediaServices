FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "src/LagoVista.VideoAssembly.Worker/LagoVista.VideoAssembly.Worker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg fontconfig curl ca-certificates \
    && mkdir -p /usr/share/fonts/truetype/manrope \
    && curl -L "https://github.com/google/fonts/raw/main/ofl/manrope/Manrope%5Bwght%5D.ttf" -o /usr/share/fonts/truetype/manrope/Manrope-VariableFont_wght.ttf \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV VideoAssembly__FfmpegPath=/usr/bin/ffmpeg
ENV VideoAssembly__FfprobePath=/usr/bin/ffprobe
ENV VideoAssembly__WorkspaceRoot=/workspace
ENV VideoAssembly__FontFamily=Manrope
ENV VideoAssembly__FontDirectory=/usr/share/fonts/truetype/manrope
VOLUME ["/workspace"]
ENTRYPOINT ["dotnet", "LagoVista.VideoAssembly.Worker.dll"]
