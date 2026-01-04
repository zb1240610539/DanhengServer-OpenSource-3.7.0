@echo off
del /s /f .\*.cs
cd ProtoFile
protoc ".\*" --csharp_out=..\