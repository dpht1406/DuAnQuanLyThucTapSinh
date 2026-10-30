import puppeteer from 'puppeteer-core'
import path from 'path'

const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe'
const OUTPUT_DIR = path.resolve('evidence/seo-job-positions')

async function captureLighthouseScreens() {
  const browser = await puppeteer.launch({
    executablePath: CHROME_PATH,
    headless: 'new',
    args: ['--no-sandbox', '--allow-file-access-from-files', '--window-size=1440,1100']
  })

  try {
    // 1. Chụp 01-lighthouse-seo-desktop.png
    console.log('--- Chụp 01-lighthouse-seo-desktop.png ---')
    const pageDesk = await browser.newPage()
    await pageDesk.setViewport({ width: 1440, height: 1050, deviceScaleFactor: 2 })
    const deskPath = 'file:///' + path.join(OUTPUT_DIR, 'lighthouse-user-desktop.html').replace(/\\/g, '/')
    await pageDesk.goto(deskPath, { waitUntil: 'networkidle0' })
    await new Promise(r => setTimeout(r, 1200))
    await pageDesk.click('.SidebarFlowStep')
    await new Promise(r => setTimeout(r, 1200))

    // Scroll to SEO section
    await pageDesk.evaluate(() => {
      const el = document.querySelector('#seo')
      if (el) el.scrollIntoView({ behavior: 'instant', block: 'start' })
    })
    await new Promise(r => setTimeout(r, 600))
    await pageDesk.screenshot({
      path: path.join(OUTPUT_DIR, '01-lighthouse-seo-desktop.png')
    })
    await pageDesk.close()
    console.log('Đã cập nhật 01-lighthouse-seo-desktop.png')

    // 2. Chụp 02-lighthouse-seo-mobile.png
    console.log('--- Chụp 02-lighthouse-seo-mobile.png ---')
    const pageMob = await browser.newPage()
    await pageMob.setViewport({ width: 1440, height: 1050, deviceScaleFactor: 2 })
    const mobPath = 'file:///' + path.join(OUTPUT_DIR, 'lighthouse-user-mobile.html').replace(/\\/g, '/')
    await pageMob.goto(mobPath, { waitUntil: 'networkidle0' })
    await new Promise(r => setTimeout(r, 1200))
    await pageMob.click('.SidebarFlowStep')
    await new Promise(r => setTimeout(r, 1200))

    // Scroll to SEO section
    await pageMob.evaluate(() => {
      const el = document.querySelector('#seo')
      if (el) el.scrollIntoView({ behavior: 'instant', block: 'start' })
    })
    await new Promise(r => setTimeout(r, 600))
    await pageMob.screenshot({
      path: path.join(OUTPUT_DIR, '02-lighthouse-seo-mobile.png')
    })
    await pageMob.close()
    console.log('Đã cập nhật 02-lighthouse-seo-mobile.png')

    // 3. Chụp 03-lighthouse-accessibility.png
    console.log('--- Chụp 03-lighthouse-accessibility.png ---')
    const pageA11y = await browser.newPage()
    await pageA11y.setViewport({ width: 1440, height: 1050, deviceScaleFactor: 2 })
    await pageA11y.goto(deskPath, { waitUntil: 'networkidle0' })
    await new Promise(r => setTimeout(r, 1200))
    await pageA11y.click('.SidebarFlowStep')
    await new Promise(r => setTimeout(r, 1200))

    // Scroll to Accessibility section
    await pageA11y.evaluate(() => {
      const el = document.querySelector('#accessibility')
      if (el) el.scrollIntoView({ behavior: 'instant', block: 'start' })
    })
    await new Promise(r => setTimeout(r, 600))
    await pageA11y.screenshot({
      path: path.join(OUTPUT_DIR, '03-lighthouse-accessibility.png')
    })
    await pageA11y.close()
    console.log('Đã cập nhật 03-lighthouse-accessibility.png')

  } finally {
    await browser.close()
  }
}

captureLighthouseScreens().catch(console.error)
