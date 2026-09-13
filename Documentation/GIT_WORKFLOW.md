# Git Workflow — 2 Developers

1. Clone repository.
2. Both developers work from develop.
3. Create feature/<short-name> from develop.
4. Small commits; one logical change per commit.
5. Push branch and open Pull Request into develop.
6. Teammate reviews and tests.
7. Merge develop -> main only at stable milestones.

Unity collaboration:
- Enable Visible Meta Files.
- Asset Serialization = Force Text.
- Use Unity Smart Merge (unityyamlmerge) for YAML conflicts.
- Never commit Library/Temp/Obj/Build/Logs.
