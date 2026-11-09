FROM ://microsoft.com AS build
WORKDIR /src

COPY ["LabTrack.csproj", "./"]
RUN dotnet restore "LabTrack.csproj"

COPY . .
RUN dotnet publish "LabTrack.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM ://microsoft.com AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LabTrack.dll"]
