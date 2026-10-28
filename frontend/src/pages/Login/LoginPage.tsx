import { useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/hooks/useAuth";
import { useAuthStore } from "@/store/authStore";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { UserRole } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

interface LoginLocationState {
  from?: string;
}

function getRolePath(role: UserRole): string {
  if (role === "Admin") {
    return "/admin";
  }

  if (role === "Manager") {
    return "/manager";
  }

  return "/operator";
}

function getPostLoginPath(role: UserRole, requestedPath?: string): string {
  if (requestedPath === "/admin" && role === "Admin") {
    return "/admin";
  }

  if (requestedPath === "/operator" && role === "Operator") {
    return "/operator";
  }

  if (requestedPath === "/manager" && role === "Manager") {
    return "/manager";
  }

  return getRolePath(role);
}

function getLoginErrorMessage(error: unknown): string {
  return getApiErrorMessage(error, "Prisijungti nepavyko. Bandykite dar kartą.");
}

export function LoginPage(): JSX.Element {
  const { login, isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const locationState = location.state as LoginLocationState | null;
  const requestedPath = locationState?.from;

  const [username, setUsername] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  if (isAuthenticated && user) {
    return <Navigate to={getPostLoginPath(user.role, requestedPath)} replace />;
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await login({
        username: username.trim(),
        password
      });

      const authenticatedUser = useAuthStore.getState().user;
      if (!authenticatedUser) {
        throw new Error("Naudotojo sesija nebuvo sukurta.");
      }

      navigate(getPostLoginPath(authenticatedUser.role, requestedPath), { replace: true });
    } catch (error) {
      setErrorMessage(getLoginErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="app-dark flex min-h-screen items-center justify-center overflow-y-auto bg-zinc-950 px-4 py-4 text-zinc-100 sm:py-8">
      <div className="w-full max-w-md rounded-md border border-zinc-800 bg-zinc-900 p-6">
        <h1 className="text-xl font-semibold text-zinc-100">QueueSystem prisijungimas</h1>
        <p className="mt-1 text-sm text-zinc-400">Prisijunkite kaip administratorius, vadovas arba operatorius.</p>

        <form className="mt-6 space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          <div className="space-y-2">
            <Label htmlFor="username">Vartotojo vardas</Label>
            <Input
              autoComplete="username"
              id="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Slaptazodis</Label>
            <Input
              autoComplete="current-password"
              id="password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          {errorMessage && (
            <div className="rounded-md border border-red-500/40 bg-red-500/15 px-3 py-2 text-sm text-red-400">{errorMessage}</div>
          )}

          <Button
            className="h-10 w-full rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300"
            disabled={isSubmitting || username.trim().length === 0 || password.length === 0}
            type="submit"
          >
            {isSubmitting ? "Jungiamasi..." : "Prisijungti"}
          </Button>
        </form>
      </div>
    </div>
  );
}
