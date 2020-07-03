# Area-of-movement-TBS-gridless-Unity
A simple visualization of the possible path of a Navmesh Agent inside Unity

By now it's an aproximation of the currently available area around a target navMesh agent

To start create a plane and set it static, bake the navigation map, then create an object and add a navmesh agent to it. At last you can add another object (like a cube), set it static aswell and add on it a nvamesh obstacle component and set the carve checkbox on. Bake the navmesh everytime you add or remove obstacles

Add these two cripts on an object (maybe the camera gameObject) and add the reference of a NavMesh Agent to the PathCalculator from the editor. You can even tweak the circle rendering result from the Inspector
