import { useLocation, useNavigate } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps): JSX.Element {
  const location = useLocation();
  const navigate = useNavigate();
  const { logout, user } = useAuth();
  const isOperator = user?.role === "Operator";
  const title = location.pathname.startsWith("/admin")
    ? "Administratoriaus skydelis"
    : location.pathname.startsWith("/manager")
      ? "Vadovo suvestinė"
      : "Operatoriaus skydelis";

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  return (
    <div className="app-dark min-h-screen bg-zinc-950 text-zinc-100">
      <div className="min-h-screen bg-zinc-950">
        {!isOperator && (
          <header className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 bg-zinc-900 px-4 py-3 sm:px-6 sm:py-4">
            <div>
              <h1 className="text-base font-semibold text-zinc-100">{title}</h1>
              <p className="text-sm text-zinc-400">{user?.username}</p>
            </div>
            <nav className="hidden items-center gap-2 md:flex">
              {user?.role === "Admin" && (
                <Button
                  className="rounded-md border-zinc-700 bg-zinc-800 text-zinc-100 hover:bg-zinc-700"
                  variant="outline"
                  onClick={() => navigate("/admin")}
                >
                  Administravimas
                </Button>
              )}
              {user?.role === "Manager" && (
                <Button
                  className="rounded-md border-zinc-700 bg-zinc-800 text-zinc-100 hover:bg-zinc-700"
                  variant="outline"
                  onClick={() => navigate("/manager")}
                >
                  Vadovo suvestinė
                </Button>
              )}
            </nav>
            <Button className="rounded-md border-zinc-700 bg-zinc-800 text-zinc-100 hover:bg-zinc-700" variant="outline" onClick={handleLogout}>
              Atsijungti
            </Button>
          </header>
        )}
        <main className={isOperator ? "min-h-screen bg-zinc-950 p-2 sm:p-3" : "min-h-[calc(100vh-73px)] bg-zinc-950 p-3 sm:p-6"}>{children}</main>
      </div>
    </div>
  );
}
