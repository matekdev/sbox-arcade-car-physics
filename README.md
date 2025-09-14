# s&box Arcade Car Physics

Ports [Arcade Car Physics](https://github.com/SergeyMakeev/ArcadeCarPhysics) to s&box. A lot of the structure is kept the same but you may see a few differences here or there. This car is also setup to work in multiplayer. The owner will have full authority over the car and the wheel visuals are networked to all clients. There is also some gizmo support but it's fairly messy.

- `Vehicle.cs` contains the properties
- `Vehicle.Gizmo.cs` contains the gizmo logic (optional)
- `Vehicle.Input.cs` contains the input handling
- `Vehicle.Physics.cs` contains the physics logic
- `Vehicle.Visual.cs` contains the logic to position and rotate the wheels (alongside networking them)

There are a lot of things that can be improved, feel free to do it yourself!
