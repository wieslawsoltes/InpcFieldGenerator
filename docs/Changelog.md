# Changelog

All notable changes to this project will be documented here. The format loosely follows [Keep a Changelog](https://keepachangelog.com/) and uses semantic versioning once releases begin.

## [Unreleased]
### Added
- Avalonia + ReactiveUI desktop sample showcasing generator output and cross-property notifications.
- Project-wide README, attribute reference, contributing guide, and changelog documentation.
- NuGet packaging metadata, icon, README, and MinVer-driven semantic versioning.
- GitHub Actions CI (build/test/pack/coverage) and release pipeline for automated publishing.
- `buildTransitive` props that default consumer projects to `LangVersion=preview` (overridable via `InpcFieldGeneratorSetLangVersion=false`).

### Changed
- Sample phase now targets Avalonia instead of WPF, aligning with cross-platform goals.
- Source generator and abstractions attributes aligned with packaging requirements and updated tests.
