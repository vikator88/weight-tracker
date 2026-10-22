import enAuth from "./en/auth.json";
import enCommon from "./en/common.json";
import enErrors from "./en/errors.json";
import esAuth from "./es/auth.json";
import esCommon from "./es/common.json";
import esErrors from "./es/errors.json";

const resources = {
  es: { auth: esAuth, common: esCommon, errors: esErrors },
  en: { auth: enAuth, common: enCommon, errors: enErrors },
};

export default resources;
