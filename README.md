MNet is networking service built using .net-core, it's meant for usage within the Unity game engine.
MNet operates on a relay like architecture where a single client is marked as the 'master client', this master client is then treated as the authority figure, the architecture is very similiar to that of Photon's Unity Networking (PUN).

MNet comes with two server executables:
- A Master Server that is used to list all regional available servers and handle versioning.
- A Game Server that is used to host player's rooms.

The intened usage is to have a single Master Server and multiple regional Game Servers, where clients can then connect to.
