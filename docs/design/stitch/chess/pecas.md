# Peças de xadrez — conjunto SVG (Stitch)

Transcrito do catálogo da folha de peças do Stitch (`pecas-folha.png`) em 2026-09-29. Cada peça é um `<svg>` independente (`viewBox 0 0 64 64`, `role="img"`, `<title>` em português). Brancas: preenchimento `#F4F6FA`, contorno ciano `#00D2D3`; pretas: preenchimento `#1B2030`, contorno vermelho `#FF4757`.

> **Defeito conhecido do mock:** na arena gerada pelo Stitch as peças pretas quase somem sobre as casas escuras e o tabuleiro mobile só renderiza a primeira fileira. A SPEC-0055 usa estes desenhos, mas define o próprio contraste (teste de tokens) e não copia o layout do mock.

## wK

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="wK">
  <title>Rei branco</title>
  <g stroke="#00D2D3" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#F4F6FA">
    <path d="M32 9 v8 M28 13 h8" />
    <path d="M22 23 C22 17, 26 17, 32 17 C38 17, 42 17, 42 23 C42 29, 46 32, 47 43 C47 46, 17 46, 17 43 C18 32, 22 29, 22 23 Z" />
    <path d="M21 34 Q32 30 43 34" fill="none" stroke-width="1.8" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## wQ

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="wQ">
  <title>Dama branca</title>
  <g stroke="#00D2D3" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#F4F6FA">
    <circle cx="16" cy="19" r="2.2" /><circle cx="24" cy="15" r="2.2" /><circle cx="32" cy="13" r="2.2" /><circle cx="40" cy="15" r="2.2" /><circle cx="48" cy="19" r="2.2" />
    <path d="M16 22 L22 36 L28 20 L32 36 L36 20 L42 36 L48 22 C46 38, 44 42, 45 44 C45 46, 19 46, 19 44 C20 42, 18 38, 16 22 Z" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## wR

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="wR">
  <title>Torre branca</title>
  <g stroke="#00D2D3" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#F4F6FA">
    <path d="M18 16 h6 v5 h5 v-5 h6 v5 h5 v-5 h6 v9 h-28 Z" />
    <path d="M21 25 L23 44 C23 46, 41 46, 41 44 L43 25 Z" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## wB

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="wB">
  <title>Bispo branco</title>
  <g stroke="#00D2D3" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#F4F6FA">
    <circle cx="32" cy="13" r="2.5" />
    <path d="M32 17 C23 17, 21 27, 25 36 C27 41, 25 43, 23 45 C23 46, 41 46, 41 45 C39 43, 37 41, 39 36 C43 27, 41 17, 32 17 Z" />
    <path d="M28 24 L36 32" stroke-width="2" fill="none" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## wN

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="wN">
  <title>Cavalo branco</title>
  <g stroke="#00D2D3" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#F4F6FA">
    <path d="M22 17 L29 14 L33 19 C37 17, 43 21, 44 26 C45 32, 42 41, 43 45 C43 46, 21 46, 21 45 C21 43, 23 39, 21 34 C19 29, 16 28, 17 25 C18 22, 23 23, 27 22 L22 17 Z" />
    <circle cx="28" cy="24" r="1.5" fill="#00D2D3" stroke="none" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## wP

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="wP">
  <title>Peão branco</title>
  <g stroke="#00D2D3" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#F4F6FA">
    <circle cx="32" cy="19" r="8" />
    <path d="M26 27 C24 35, 23 41, 23 45 C23 46, 41 46, 41 45 C41 41, 40 35, 38 27 Z" />
    <rect x="18" y="47" width="28" height="5" rx="2" />
    <rect x="14" y="53" width="36" height="5" rx="2" />
  </g>
</svg>
```

## bK

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="bK">
  <title>Rei preto</title>
  <g stroke="#FF4757" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#1B2030">
    <path d="M32 9 v8 M28 13 h8" />
    <path d="M22 23 C22 17, 26 17, 32 17 C38 17, 42 17, 42 23 C42 29, 46 32, 47 43 C47 46, 17 46, 17 43 C18 32, 22 29, 22 23 Z" />
    <path d="M21 34 Q32 30 43 34" fill="none" stroke-width="1.8" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## bQ

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="bQ">
  <title>Dama preta</title>
  <g stroke="#FF4757" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#1B2030">
    <circle cx="16" cy="19" r="2.2" /><circle cx="24" cy="15" r="2.2" /><circle cx="32" cy="13" r="2.2" /><circle cx="40" cy="15" r="2.2" /><circle cx="48" cy="19" r="2.2" />
    <path d="M16 22 L22 36 L28 20 L32 36 L36 20 L42 36 L48 22 C46 38, 44 42, 45 44 C45 46, 19 46, 19 44 C20 42, 18 38, 16 22 Z" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## bR

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="bR">
  <title>Torre preta</title>
  <g stroke="#FF4757" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#1B2030">
    <path d="M18 16 h6 v5 h5 v-5 h6 v5 h5 v-5 h6 v9 h-28 Z" />
    <path d="M21 25 L23 44 C23 46, 41 46, 41 44 L43 25 Z" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## bB

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="bB">
  <title>Bispo preto</title>
  <g stroke="#FF4757" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#1B2030">
    <circle cx="32" cy="13" r="2.5" />
    <path d="M32 17 C23 17, 21 27, 25 36 C27 41, 25 43, 23 45 C23 46, 41 46, 41 45 C39 43, 37 41, 39 36 C43 27, 41 17, 32 17 Z" />
    <path d="M28 24 L36 32" stroke-width="2" fill="none" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## bN

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="bN">
  <title>Cavalo preto</title>
  <g stroke="#FF4757" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#1B2030">
    <path d="M22 17 L29 14 L33 19 C37 17, 43 21, 44 26 C45 32, 42 41, 43 45 C43 46, 21 46, 21 45 C21 43, 23 39, 21 34 C19 29, 16 28, 17 25 C18 22, 23 23, 27 22 L22 17 Z" />
    <circle cx="28" cy="24" r="1.5" fill="#FF4757" stroke="none" />
    <rect x="15" y="47" width="34" height="5" rx="2" />
    <rect x="12" y="53" width="40" height="5" rx="2" />
  </g>
</svg>
```

## bP

```xml
<svg role="img" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" data-piece="bP">
  <title>Peão preto</title>
  <g stroke="#FF4757" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" fill="#1B2030">
    <circle cx="32" cy="19" r="8" />
    <path d="M26 27 C24 35, 23 41, 23 45 C23 46, 41 46, 41 45 C41 41, 40 35, 38 27 Z" />
    <rect x="18" y="47" width="28" height="5" rx="2" />
    <rect x="14" y="53" width="36" height="5" rx="2" />
  </g>
</svg>
```
