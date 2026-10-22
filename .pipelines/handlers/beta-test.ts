import { implement } from "@olve/pipelines";
import { betaTest } from "../pipeline";
import { smokeTest } from "./lib/smoke";

export default implement(betaTest, ({ release }, ctx) => smokeTest(ctx, release));
