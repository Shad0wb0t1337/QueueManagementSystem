import axios from "axios";

interface ApiErrorResponse {
  error?: unknown;
  message?: unknown;
  title?: unknown;
  errors?: unknown;
}

const fallbackErrorMessage = "Įvyko klaida. Bandykite dar kartą.";

const knownMessageTranslations: Array<[string, string]> = [
  ["Cannot issue ticket because this service type has no active enabled counters.", "Paslauga šiuo metu neaptarnaujama."],
  ["Cannot issue ticket for disabled service type.", "Paslauga šiuo metu neaptarnaujama."],
  ["No waiting ticket found for this counter.", "Nėra laukiančių bilietų šiam langeliui."],
  ["No services are enabled for this counter.", "Šiam langeliui neįjungta nė viena paslauga."],
  ["Call Next is allowed only when counter status is Active.", "Kviesti galima tik aktyviame langelyje."],
  ["Ticket can be moved to Serving only from Called status.", "Aptarnauti galima tik iškviestą bilietą."],
  ["Ticket can be marked Done only from Called or Serving status.", "Užbaigti galima tik iškviestą arba aptarnaujamą bilietą."],
  ["Ticket can be marked NoShow only from Called or Serving status.", "Neatvykusiu galima žymėti tik iškviestą arba aptarnaujamą bilietą."],
  ["Operator can act only on tickets assigned to own counter.", "Šis bilietas nepriklauso jūsų langeliui."],
  ["Operator can call next ticket only for own counter.", "Kviesti galite tik savo langelio bilietus."],
  ["Only operators can perform queue actions.", "Šį veiksmą gali atlikti tik operatorius."],
  ["Only operators can update service profile.", "Paslaugų profilį gali keisti tik operatorius."],
  ["Only operators can update own counter status.", "Langelio būseną gali keisti tik operatorius."],
  ["Operator can update only own counter service profile.", "Galite keisti tik savo langelio paslaugas."],
  ["Manager is not assigned to the requested department.", "Neturite teisės peržiūrėti šio skyriaus duomenų."],
  ["Only managers can view manager statistics.", "Neturite teisės peržiūrėti vadovo statistikos."],
  ["Department assignments can be created only for Manager users.", "Skyrius galima priskirti tik vadovo rolei."],
  ["Inactive departments cannot be assigned to managers.", "Neaktyvių skyrių priskirti vadovams negalima."],
  ["Inactive departments cannot be assigned to counters.", "Neaktyvaus skyriaus priskirti langeliui negalima."],
  ["Counter display order must be positive.", "Ekrano eilės numeris turi būti teigiamas."],
  ["Counter display order must be unique.", "Ekrano eilės numeris turi būti unikalus."],
  ["Counter display range is invalid.", "Neteisingas langelių intervalas."],
  ["Both from and to range values are required.", "Neteisingas langelių intervalas."],
  ["Invalid username or password.", "Nepavyko prisijungti. Patikrinkite prisijungimo duomenis."],
  ["Invalid token user id.", "Prisijungimo sesija baigėsi. Prisijunkite iš naujo."],
  ["An unexpected error occurred.", fallbackErrorMessage],
  ["Internal Server Error", fallbackErrorMessage]
];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getStringValue(value: unknown): string | null {
  return typeof value === "string" && value.trim().length > 0 ? value.trim() : null;
}

function getValidationMessage(errors: unknown): string | null {
  if (!isRecord(errors)) {
    return null;
  }

  const messages: string[] = [];
  for (const value of Object.values(errors)) {
    if (typeof value === "string" && value.trim().length > 0) {
      messages.push(value.trim());
    } else if (Array.isArray(value)) {
      const firstMessage = value.find((item): item is string => typeof item === "string" && item.trim().length > 0);
      if (firstMessage) {
        messages.push(firstMessage.trim());
      }
    }
  }

  return messages.length > 0 ? messages.slice(0, 3).join(" ") : null;
}

function translateKnownMessage(message: string): string {
  if (message.startsWith("Validation failed:")) {
    return message.replace("Validation failed:", "Patikrinkite įvestus duomenis:");
  }

  if (/^Counter \d+ was not found\.$/.test(message)) {
    return "Langelis nerastas.";
  }

  if (/^Ticket \d+ was not found\.$/.test(message)) {
    return "Bilietas nerastas.";
  }

  if (/^Service type \d+ was not found\.$/.test(message)) {
    return "Paslauga nerasta.";
  }

  if (/^Department \d+ was not found\.$/.test(message)) {
    return "Skyrius nerastas.";
  }

  const translation = knownMessageTranslations.find(([technicalMessage]) => message.includes(technicalMessage));
  return translation ? translation[1] : message;
}

function getBackendMessage(data: unknown): string | null {
  if (!isRecord(data)) {
    return null;
  }

  const validationMessage = getValidationMessage(data.errors);
  if (validationMessage) {
    return translateKnownMessage(validationMessage);
  }

  const message = getStringValue(data.message) ?? getStringValue(data.error) ?? getStringValue(data.title);
  return message ? translateKnownMessage(message) : null;
}

export function getApiErrorMessage(error: unknown, fallbackMessage = fallbackErrorMessage): string {
  if (!axios.isAxiosError<ApiErrorResponse>(error)) {
    return fallbackMessage;
  }

  if (!error.response) {
    return "Nepavyko prisijungti prie serverio.";
  }

  const backendMessage = getBackendMessage(error.response.data);
  if (backendMessage) {
    return backendMessage;
  }

  if (error.response.status === 401) {
    return "Prisijungimo sesija baigėsi. Prisijunkite iš naujo.";
  }

  if (error.response.status === 403) {
    return "Neturite teisės atlikti šio veiksmo.";
  }

  if (error.response.status === 404) {
    return "Įrašas nerastas.";
  }

  return fallbackMessage;
}
