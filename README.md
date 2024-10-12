create a new godot 4 project normally, through the editor. Be sure to setup VS as your external editor in the godot options.

create your C# script attached to a node through the editor (as normal). This will cause the editor to auto generate a csproj+solution for you.

Test build/run via the godot editor, to make sure it all works.

In Visual Studio, create a new Launch Profile for an Executable

Use the dropdown next to the green Run button, then YourProject Debug Properties. There will be a button at the top-left for adding a Launch Profile.

set the executable path to a relative path to the godot binary, from your csproj's location. example: ..\..\bin\Godot_v4.0-beta8_mono_win64\Godot_v4.0-beta8_mono_win64.exe

set the command line arguments to simply startup the project in the current directory. example: --path . --verbose

set the working directory to the current. example: .

Set enable native code debugging if you want to see better errors in the output window. Leaving this disabled allows hot-reload to work (!!!) but various godot cpp errors won't be shown.

Then if you choose that debug profile, hit F5 and it works! Breakpoints work too.