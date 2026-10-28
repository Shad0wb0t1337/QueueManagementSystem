import axios from "axios";
import { toast } from "@/hooks/use-toast";
import { getApiErrorMessage } from "@/utils/apiError";

const baseURL = import.meta.env.VITE_API_URL ?? "http://localhost:5120";

export const api = axios.create({
  baseURL,
  headers: {
    "Content-Type": "application/json"
  }
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("auth_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    const message = getApiErrorMessage(error);

    toast({
      title: "Užklausa nepavyko",
      description: message,
      variant: "destructive"
    });

    return Promise.reject(error);
  }
);
