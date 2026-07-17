
FROM mcr.microsoft.com/dotnet/runtime:9.0-bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg fontconfig curl ca-certificates \
    && mkdir -p /usr/share/fonts/truetype/manrope \
    && curl -L "https://github.com/google/fonts/raw/main/ofl/manrope/Manrope%5Bwght%5D.ttf" -o /usr/share/fonts/truetype/manrope/Manrope-VariableFont_wght.ttf \
    && fc-cache -f \
    && mkdir -p /workspace \
    && chown -R 10001:10001 /workspace \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app

COPY --chown=10001:10001 ./publish .


ENV VideoAssembly__FfmpegPath=/usr/bin/ffmpeg
ENV VideoAssembly__FfprobePath=/usr/bin/ffprobe
ENV VideoAssembly__WorkspaceRoot=/workspace
ENV VideoAssembly__FontFamily=Manrope
ENV VideoAssembly__FontDirectory=/usr/share/fonts/truetype/manrope
VOLUME ["/workspace"]
USER 10001:10001
ENTRYPOINT ["dotnet", "LagoVista.VideoAssembly.Worker.dll"]
