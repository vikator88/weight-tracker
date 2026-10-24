import enAuth from "./en/auth.json";
import enCommon from "./en/common.json";
import enErrors from "./en/errors.json";
import enOnboarding from "./en/onboarding.json";
import enProfile from "./en/profile.json";
import esAuth from "./es/auth.json";
import esCommon from "./es/common.json";
import esErrors from "./es/errors.json";
import esOnboarding from "./es/onboarding.json";
import esProfile from "./es/profile.json";

const resources = {
  es: {
    auth: esAuth,
    common: esCommon,
    errors: esErrors,
    onboarding: esOnboarding,
    profile: esProfile,
  },
  en: {
    auth: enAuth,
    common: enCommon,
    errors: enErrors,
    onboarding: enOnboarding,
    profile: enProfile,
  },
};

export default resources;
