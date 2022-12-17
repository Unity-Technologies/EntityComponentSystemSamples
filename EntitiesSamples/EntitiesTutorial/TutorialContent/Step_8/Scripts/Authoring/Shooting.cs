using Unity.Entities;

// This is a tag component that is also an "enableable component".
// Such components can be toggled on and off while remaining present on the entity.
// Doing so is a lot more efficient than adding and removing the component.
struct Shooting : IComponentData, IEnableableComponent
{
}