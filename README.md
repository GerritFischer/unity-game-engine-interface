## **Unity Simulator** ##

AI Study Project WS24/25:
Development of an interface between the Unity game engine and a reinforcement learning framework
(Re-uploaded for archival purposes)
------------------------------------------------------
**About**

A 3D simulator created in Unity Game Engine to be used with CoBeL-RL [(Diekmann et al., 2023)](https://doi.org/10.3389/fninf.2023.1134405).

-----------------------------------------------------------
**Setup Guide**

1. Download or clone the project using git.

2. Install the [Unity Hub](https://docs.unity3d.com/hub/manual/InstallHub.html)

3. Install a Unity Version (developed and tested on 2022.3.49f1)

</details>

--------------------------------------------------------------------------------

**Builds**

Prebuild simulators are available for Windows and Linux on ...

If you wish to build the simulator yourself follow the instructions on the [official documentation](https://docs.unity.com).

The Unity project files are found in the ``unity-simulator`` directory.

-------------------------------------------------------------------------------------------

**Making your own environments**

In order to use scenes you have created in Unity ensure the follwing:

1. Your scene must be packed into an [AssetBundle](https://docs.unity3d.com/2022.3/Documentation/Manual/AssetBundles-Workflow.html). This AssetBundle can contain multiple scenes.

2. Ensure that the names of the game objects do not clash with the predefined names (agent, camera_n, camera_e, camera_s and camera_w).

-------------------------------------------------------------------------------------------

**List of publications**

* Diekmann, N., Vijayabaskaran, S., Zeng, X., Kappel, D., Menezes, M. C., & Cheng, S. (2023). CoBeL-RL: A neuroscience-oriented simulation framework for complex behavior and learning. In Frontiers in Neuroinformatics (Vol. 17). Frontiers Media SA. [[DOI]](https://doi.org/10.3389/fninf.2023.1134405)

--------------------------------------------------------------------------------------------
**Code Contributors**

Present Contributors: Diekmann, N.

Past Contributors : NA

Students : Fischer, G.S., Tran, D.C.T., Wojak, M.
