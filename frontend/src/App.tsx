import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { Toaster } from "@/components/ui/toaster";
import { ErrorBoundary } from "@/components/shared/ErrorBoundary";
import { Layout } from "@/components/shared/Layout";
import { ProtectedRoute } from "@/components/shared/ProtectedRoute";
import { HomePage } from "@/pages/Home/HomePage";
import { KioskPage } from "@/pages/Kiosk/KioskPage";
import { DisplayPage } from "@/pages/Display/DisplayPage";
import { CounterGridDisplayPage } from "@/pages/Display/CounterGridDisplayPage";
import { CounterDisplayPage } from "@/pages/CounterDisplay/CounterDisplayPage";
import { OperatorPage } from "@/pages/Operator/OperatorPage";
import { AdminPage } from "@/pages/Admin/AdminPage";
import { ManagerPage } from "@/pages/Manager/ManagerPage";
import { LoginPage } from "@/pages/Login/LoginPage";
import { useThemeMode } from "@/hooks/useThemeMode";

function ThemeModeController(): null {
  useThemeMode();
  return null;
}

function App(): JSX.Element {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <ThemeModeController />
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/kiosk" element={<KioskPage />} />
          <Route path="/display" element={<DisplayPage />} />
          <Route path="/display1" element={<DisplayPage />} />
          <Route path="/display2" element={<CounterGridDisplayPage />} />
          <Route path="/display2/:range" element={<CounterGridDisplayPage />} />
          <Route path="/counter-display/:counterId" element={<CounterDisplayPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/operator"
            element={
              <ProtectedRoute requiredRole="Operator">
                <Layout>
                  <OperatorPage />
                </Layout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/admin"
            element={
              <ProtectedRoute requiredRole="Admin">
                <Layout>
                  <AdminPage />
                </Layout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/manager"
            element={
              <ProtectedRoute requiredRole="Manager">
                <Layout>
                  <ManagerPage />
                </Layout>
              </ProtectedRoute>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
        <Toaster />
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default App;
