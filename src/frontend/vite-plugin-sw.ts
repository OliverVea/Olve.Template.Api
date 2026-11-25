import { readdir, readFile, writeFile } from "node:fs/promises";
import { join, relative, resolve, sep } from "node:path";
import type { Plugin } from "vite";
import { type BuiltFile, serviceWorkerSource } from "./src/pwa/service-worker-source.ts";

/**
 * Writes dist/sw.js once the build is on disk (the public/ files included), precaching every built
 * file under a cache named for the build's version. Build only: under `bun run dev` there is no
 * worker.
 */
export function serviceWorker(): Plugin {
  let outDir = "";
  let srcDir = "";
  return {
    name: "olve-service-worker",
    apply: "build",
    configResolved(config) {
      outDir = resolve(config.root, config.build.outDir);
      srcDir = resolve(config.root, "src/sw");
    },
    async closeBundle() {
      const files: BuiltFile[] = [];
      for (const entry of await readdir(outDir, { recursive: true, withFileTypes: true })) {
        if (!entry.isFile()) continue;
        const path = join(entry.parentPath, entry.name);
        files.push({
          path: relative(outDir, path).split(sep).join("/"),
          content: await readFile(path),
        });
      }
      const worker = await serviceWorkerSource(
        files,
        await readFile(join(srcDir, "shell.js"), "utf8"),
        await readFile(join(srcDir, "extensions.js"), "utf8"),
      );
      await writeFile(join(outDir, "sw.js"), worker.source);
      this.info?.(`sw.js ${worker.version}: ${worker.precache.length} files precached`);
    },
  };
}
