using SHARMemory.SHAR;
using SHARMemory.SHAR.Classes;
using SHARMemory.SHAR.Pointers;
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
    await RunRainbowTraffic(memory);

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

void SetShaderDiffuse(Shader? shader, Color col)
{
    if (shader == null)
        return;

    d3dShader d3dShader = shader.D3DShader;
    if (d3dShader == null)
        return;

    d3dShaderInfo shaderInfo = d3dShader.ShaderInfo;
    shaderInfo.Diffuse = new(col);
    d3dShader.ShaderInfo = shaderInfo;
}

void ColourDrawable(Drawable? drawable, Color col)
{
    if (drawable == null)
        return;

    switch (drawable.Type)
    {
        case Drawable.Types.Mesh:
            Mesh mesh = drawable.ReinterpretCast<Mesh>();
            PointerArray<PrimGroup> primGroups = mesh.PrimGroups;
            foreach (PrimGroup primGroup in primGroups)
                SetShaderDiffuse(primGroup?.Shader, col);
            break;
        case Drawable.Types.TrafficBodyDrawable:
            ColourDrawable(drawable.ReinterpretCast<TrafficBodyDrawable>().BodyPropDrawable, col);
            break;
    }
}

async Task RunRainbowTraffic(Memory memory)
{
    while (!memory.Process.HasExited)
    {
        try
        {
            GameFlow.GameState state = memory.GameFlow.State;
            if (state == GameFlow.GameState.DemoInGame || state == GameFlow.GameState.NormalInGame || state == GameFlow.GameState.BonusInGame)
            {
                VehicleCentral vehicleCentral = memory.VehicleCentral;
                if (vehicleCentral.IsPointerValid)
                {
                    Color col = Rainbow(4);
                    foreach (Vehicle vehicle in vehicleCentral.ActiveVehicles)
                    {
                        vehicle.SetTrafficBodyColour(col);

                        PointerArray<DrawableElement>? elements = vehicle.GeometryVehicle?.CompositeDrawable?.Elements;
                        if (elements == null)
                            continue;

                        foreach (DrawableElement element in elements)
                        {
                            if (element.Type != DrawableElement.Types.Prop)
                                continue;

                            DrawablePropElement propElement = element.ReinterpretCast<DrawablePropElement>();
                            ColourDrawable(propElement.Drawable, col);
                        }
                    }
                }
            }

            await Task.Delay(10);
        }
        catch (Exception ex)
        {
            HandleError(ex, "RunRainbowTraffic");
            return;
        }
    }
}

void WriteLog(string message, string method) => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] <{method}> {message}");

void HandleError(Exception ex, string method) => WriteLog($"There was an error in \"{method}\": {ex}", "HandleError");