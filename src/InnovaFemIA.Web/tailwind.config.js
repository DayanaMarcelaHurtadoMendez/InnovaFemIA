/** @type {import('tailwindcss').Config} */
module.exports = {
  // Escanea todos los archivos .cshtml, .cs y .js para detectar clases usadas
  content: [
    "./Views/**/*.cshtml",
    "./Features/**/*.cshtml",
    "./Features/**/*.cs",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      // ─── Paleta de colores InnovaFemIA ───────────────────────────────
      colors: {
        // Verde primario (naturaleza, agricultura, ASCAMPESINA)
        primary: {
          50:  '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d',
          950: '#052e16',
        },
        // Tierra/Ámbar secundario (cultivos, cosecha)
        secondary: {
          50:  '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
        },
        // Morado/Orquídea (identidad de marca - Proyecto Orquídeas)
        brand: {
          50:  '#faf5ff',
          100: '#f3e8ff',
          200: '#e9d5ff',
          300: '#d8b4fe',
          400: '#c084fc',
          500: '#a855f7',
          600: '#9333ea',
          700: '#7e22ce',
          800: '#6b21a8',
          900: '#581c87',
        },
        // Superficie oscura para glassmorphism y dark elements
        surface: {
          dark:   '#0f1117',
          darker: '#090b0f',
          card:   'rgba(255, 255, 255, 0.08)',
        }
      },
      // ─── Tipografía ──────────────────────────────────────────────────
      fontFamily: {
        sans: ['Outfit', 'Inter', 'system-ui', 'sans-serif'],
        display: ['Outfit', 'sans-serif'],
      },
      // ─── Animaciones micro-interacción ───────────────────────────────
      keyframes: {
        'fade-in': {
          '0%': { opacity: '0', transform: 'translateY(8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-up': {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'pulse-soft': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' },
        },
        'luna-glow': {
          '0%, 100%': { boxShadow: '0 0 8px rgba(168, 85, 247, 0.4)' },
          '50%': { boxShadow: '0 0 24px rgba(168, 85, 247, 0.8)' },
        }
      },
      animation: {
        'fade-in': 'fade-in 0.4s ease-out',
        'slide-up': 'slide-up 0.5s ease-out',
        'pulse-soft': 'pulse-soft 2s ease-in-out infinite',
        'luna-glow': 'luna-glow 2s ease-in-out infinite',
      },
      // ─── Sombras glassmorphism ────────────────────────────────────────
      boxShadow: {
        'glass': '0 8px 32px 0 rgba(31, 38, 135, 0.15)',
        'card': '0 4px 24px rgba(0, 0, 0, 0.08)',
        'card-hover': '0 12px 40px rgba(0, 0, 0, 0.15)',
      },
      // ─── Border radius ────────────────────────────────────────────────
      borderRadius: {
        'xl': '0.875rem',
        '2xl': '1.25rem',
        '3xl': '1.75rem',
      },
    },
  },
  plugins: [],
}
