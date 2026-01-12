/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
        },
        government: {
          50: '#f0f9ff',
          100: '#e0f2fe',
          500: '#0369a1',
          600: '#0c4a6e',
          700: '#0e3a57',
          800: '#082f49',
          900: '#0c1829',
        }
      },
    },
  },
  plugins: [],
}
