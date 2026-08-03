/**
 * build.mjs — Export Cursus LinkedIn carousel to PDF (10 × 1080×1350)
 *
 * Usage: node build.mjs
 * Output: linkedin-carousel/cursus-linkedin-carousel.pdf
 */

import fs from "node:fs";
import path from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const HUB_PDF = "/home/abdo/Dev/Student-Hub/docs/pdf-build";
const puppeteer = require(path.join(HUB_PDF, "node_modules/puppeteer"));

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(__dirname, "cursus-linkedin-carousel.pdf");
const HTML = path.join(__dirname, "index.html");

const CHROME = path.join(
  HUB_PDF,
  ".cache/chrome/linux-127.0.6533.88/chrome-linux64/chrome"
);

async function main() {
  if (!fs.existsSync(HTML)) {
    console.error("Missing index.html at", HTML);
    process.exit(1);
  }

  const launchOpts = {
    headless: "new",
    args: ["--no-sandbox", "--disable-setuid-sandbox", "--allow-file-access-from-files"],
  };
  if (fs.existsSync(CHROME)) {
    launchOpts.executablePath = CHROME;
  }

  const browser = await puppeteer.launch(launchOpts);
  try {
    const page = await browser.newPage();
    await page.goto(`file://${HTML}`, { waitUntil: "networkidle0", timeout: 60_000 });
    await page.evaluateHandle("document.fonts.ready");

    await page.pdf({
      path: OUT,
      width: "1080px",
      height: "1350px",
      printBackground: true,
      pageRanges: "1-10",
      margin: { top: 0, right: 0, bottom: 0, left: 0 },
    });

    const stat = fs.statSync(OUT);
    const mb = (stat.size / (1024 * 1024)).toFixed(2);
    console.log(`✅ PDF written: ${OUT} (${mb} MB)`);
    if (stat.size > 10 * 1024 * 1024) {
      console.warn("⚠️  File exceeds 10 MB — consider further image compression.");
    }
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
