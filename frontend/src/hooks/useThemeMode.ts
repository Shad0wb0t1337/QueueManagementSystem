import { useEffect } from "react";
import { useLocation } from "react-router-dom";

type ThemeMode = "default" | "dark" | "light";

const themeStorageKey = "queue-theme-mode";

function isThemeMode(value: string | null): value is ThemeMode {
  return value === "default" || value === "dark" || value === "light";
}

function getStoredThemeMode(): ThemeMode {
  const storedValue = window.localStorage.getItem(themeStorageKey);
  return isThemeMode(storedValue) ? storedValue : "default";
}

function applyThemeMode(mode: ThemeMode): void {
  const root = document.documentElement;
  root.classList.remove("theme-default", "theme-dark", "theme-light", "dark");
  root.classList.add(`theme-${mode}`);

  if (mode === "default" || mode === "dark") {
    root.classList.add("dark");
  }
}

export function useThemeMode(): void {
  const location = useLocation();

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const urlTheme = params.get("theme");

    if (isThemeMode(urlTheme)) {
      if (urlTheme === "default") {
        window.localStorage.removeItem(themeStorageKey);
      } else {
        window.localStorage.setItem(themeStorageKey, urlTheme);
      }

      applyThemeMode(urlTheme);
      return;
    }

    applyThemeMode(getStoredThemeMode());
  }, [location.search]);
}
