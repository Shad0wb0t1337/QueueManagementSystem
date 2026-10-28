import { create } from "zustand";
import { persist } from "zustand/middleware";
import { login as loginRequest, me as meRequest } from "@/services/authService";
import type { AuthResponse, AuthUser, LoginRequest } from "@/types";

interface AuthStoreState {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
  loadMe: () => Promise<void>;
  setSession: (response: AuthResponse) => void;
}

function mapAuthResponseToUser(response: AuthResponse): AuthUser {
  return {
    username: response.username,
    role: response.role,
    operatorId: response.operatorId,
    operatorName: response.operatorName,
    counterId: response.counterId,
    counterName: response.counterName,
    counterStatus: response.counterStatus,
    serviceTypeId: response.serviceTypeId,
    serviceTypeName: response.serviceTypeName,
    supportedServiceTypeIds: response.supportedServiceTypeIds ?? [],
    supportedServiceTypes: response.supportedServiceTypes ?? []
  };
}

export const useAuthStore = create<AuthStoreState>()(
  persist(
    (set, get) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      login: async (request: LoginRequest) => {
        const response = await loginRequest(request);
        get().setSession(response);
      },
      logout: () => {
        set({ user: null, token: null, isAuthenticated: false });
      },
      loadMe: async () => {
        if (!get().token) {
          return;
        }

        const response = await meRequest();
        set({
          user: mapAuthResponseToUser(response),
          token: get().token,
          isAuthenticated: true
        });
      },
      setSession: (response: AuthResponse) => {
        set({
          user: mapAuthResponseToUser(response),
          token: response.token,
          isAuthenticated: true
        });
      }
    }),
    {
      name: "queuesystem-auth",
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        isAuthenticated: state.isAuthenticated
      }),
      onRehydrateStorage: () => (state) => {
        if (state?.token) {
          localStorage.setItem("auth_token", state.token);
        }
      }
    }
  )
);

useAuthStore.subscribe((state) => {
  if (state.token) {
    localStorage.setItem("auth_token", state.token);
  } else {
    localStorage.removeItem("auth_token");
  }
});
