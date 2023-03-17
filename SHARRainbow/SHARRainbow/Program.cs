using SHARMemory.Memory;
using SHARMemory.SHAR;
using SHARMemory.SHAR.Classes;
using SHARMemory.SHAR.Structs;
using System.Diagnostics;
using System.Drawing;

Process? p;
WriteLog("Waiting for SHAR process...", "Main");
while (true)
{
    do
    {
        p = Memory.GetSHARProcess();
    } while (p == null);

    WriteLog("Found SHAR process. Initialising memory manager...", "Main");

    Memory memory = new(p);
    WriteLog($"SHAR memory manager initialised. Game version detected: {memory.GameVersion}. Language: {memory.GameSubVersion}.", "Main");
    await RunRainbow(memory);

    memory.Dispose();
    p.Dispose();
    WriteLog("SHAR closed. Waiting for SHAR process...", "Main");
}

Color Rainbow(float speed)
{
    double d = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
    byte r = (byte)Math.Floor(Math.Sin(d * speed + 0) * 127 + 128);
    byte g = (byte)Math.Floor(Math.Sin(d * speed + 2) * 127 + 128);
    byte b = (byte)Math.Floor(Math.Sin(d * speed + 4) * 127 + 128);
    return Color.FromArgb(r, g, b);
}

Color MultiplyColours(Color color1, Color color2) => Color.FromArgb(color1.A * color2.A / 255, color1.R * color2.R / 255, color1.G * color2.G / 255, color1.B * color2.B / 255);

async Task RunRainbow(Memory memory)
{
    while (!memory.Process.HasExited)
    {
        try
        {
            UpdateLights(memory);

            await Task.Delay(10);
        }
        catch (Exception ex)
        {
            HandleError(ex, "RunRainbow");
            return;
        }
    }
}

void UpdateLights(Memory memory)
{
    GameFlow.GameState? state = memory.Singletons.GameFlow?.State;
    if (state == null || !(state == GameFlow.GameState.DemoInGame || state == GameFlow.GameState.NormalInGame || state == GameFlow.GameState.BonusInGame))
        return;

    RenderManager renderManager = memory.Singletons.RenderManager;
    if (renderManager == null)
        return;

    MoodLighting mood = renderManager.Mood;
    mood.Transition = -1f;
    memory.Singletons.RenderManager.Mood = mood;

    PointerArray<Light>? lights = mood.SunGroup?.Lights;
    if (lights == null)
        return;
    
    Color col = Rainbow(4);
    for (uint i = 0; i < lights.Count; i++)
    {
        Light light = lights[i];
        if (light != null && light is not AmbientLight)
            light.Colour = MultiplyColours(col, mood.Originals[i]);
    }
}

void WriteLog(string message, string method) => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] <{method}> {message}");

void HandleError(Exception ex, string method) => WriteLog($"There was an error in \"{method}\": {ex}", "HandleError");