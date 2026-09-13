# Tiny Tactics — Project Structure

Assets/_Project = first-party game content.
Assets/ThirdParty = external assets/packages only.

Scripts: Core, Game, Units, Buildings, Combat, Economy, AI, Input, Camera, UI, Utilities.
ScriptableObjects: Units, Buildings, Resources, Game.
Prefabs: Units, Buildings, Environment, UI.
Scenes: Bootstrap, Gameplay, UI.

Naming:
- C# types/methods/properties: PascalCase
- private fields: _camelCase
- Unit_<Name>, Building_<Name>, Resource_<Name>
- Bootstrap, MainMenu, Game_<Mode>
