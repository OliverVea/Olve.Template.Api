import { implement } from "@olivervea/pipelines";
import { prodTest } from "../pipeline";
import { smokeTest } from "./lib/smoke";

export default implement(prodTest, ({ release }, ctx) => smokeTest(ctx, release));
