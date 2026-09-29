# Changelog

Todas as mudanças relevantes deste projeto são documentadas aqui.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/)
e o projeto adota [Versionamento Semântico](https://semver.org/lang/pt-BR/).
Cada entrada cita a spec de origem (`SPEC-NNNN`); gere-as com `spec_graph.py report SPEC-NNNN --changelog`.

## [Unreleased]

### Added
- Arena da partida no visual Cyber Arena com linha vencedora (SPEC-0031)
- Ranking no visual Cyber Arena com pódio e tabela (SPEC-0033)
- Histórico de partidas no visual Cyber Arena (SPEC-0032)
- Lobby no visual Cyber Arena com cartões de modo, dificuldade e sala privada (SPEC-0030)
- Shell Cyber Arena responsivo e primitivos de UI (SPEC-0043)
- Pipeline do Tailwind CSS standalone com tokens Cyber Arena e fontes locais (SPEC-0029)
- Timer de turno e timeout por W.O. (SPEC-0027)

### Fixed
- Gravação única do resultado da partida (SPEC-0045)

### Removed
- MudBlazor, Bootstrap, página de exemplo `/counter` e menu lateral legado; reset de CSS (preflight) do Tailwind habilitado (SPEC-0034)
