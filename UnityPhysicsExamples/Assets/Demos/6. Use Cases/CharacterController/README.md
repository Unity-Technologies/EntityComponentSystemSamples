Tried adding FBX from Blender
 - can't add Physics Components to parent node. Had to put mesh on mesh node itself.
 - scaling not considered
 - mesh collider needs Mesh Instance Renderer component?
 -- which doesn't render in the position requested grr.

gravity component defaults to 0
I can set mass values to zero?

why doesn't scale get copied from initial transform


// nasty way of telling a static/dynamic body.
// what about keyframed?
if (hit && rayResult.RigidBodyIndex < world.MotionDatas.Length) 