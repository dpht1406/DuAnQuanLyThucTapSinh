import puppeteer from 'puppeteer-core'
import { startFlow } from 'lighthouse'
import path from 'path'
import fs from 'fs'

const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe'
const BASE_URL = 'http://localhost:5173'
const API_URL = 'http://localhost:5018/api'
const OUTPUT_DIR = path.resolve('evidence/seo-job-positions')

if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true })
}

async function getTokens(username, password) {
  const res = await fetch(`${API_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  })
  if (!res.ok) {
    throw new Error(`Login failed for ${username}: ${res.status} ${res.statusText}`)
  }
  const json = await res.json()
  return {
    accessToken: json.data.accessToken,
    refreshToken: json.data.refreshToken,
    user: json.data.user
  }
}

async function setupAuth(page, tokens) {
  await page.goto(BASE_URL, { waitUntil: 'domcontentloaded' })
  await page.evaluate((tok, ref) => {
    localStorage.setItem('accessToken', tok)
    localStorage.setItem('refreshToken', ref)
  }, tokens.accessToken, tokens.refreshToken)
}

async function runLighthouseAudit(browser, tokens, isMobile, reportPrefix) {
  const page = await browser.newPage()
  await setupAuth(page, tokens)

  const width = isMobile ? 390 : 1350
  const height = isMobile ? 844 : 940

  const flow = await startFlow(page, {
    name: `${reportPrefix} Flow`,
    flags: {
      screenEmulation: {
        disabled: false,
        width,
        height,
        deviceScaleFactor: isMobile ? 2 : 1,
        mobile: isMobile
      },
      formFactor: isMobile ? 'mobile' : 'desktop'
    }
  })

  await flow.navigate(`${BASE_URL}/job-positions`, {
    stepName: `Job Positions ${isMobile ? 'Mobile' : 'Desktop'}`
  })

  const reportHtml = await flow.generateReport()
  const flowResult = await flow.createFlowResult()
  const lhr = flowResult.steps[0].lhr

  const htmlPath = path.join(OUTPUT_DIR, `${reportPrefix}.html`)
  const jsonPath = path.join(OUTPUT_DIR, `${reportPrefix}.json`)

  fs.writeFileSync(htmlPath, reportHtml, 'utf8')
  fs.writeFileSync(jsonPath, JSON.stringify(lhr, null, 2), 'utf8')

  await page.close()
  return { lhr, htmlPath, jsonPath }
}

async function main() {
  console.log('--- 1. LẤY TOKEN ĐĂNG NHẬP CHO USER VÀ ADMIN ---')
  const userTokens = await getTokens('PI02099', '123456')
  const adminTokens = await getTokens('admin', 'Admin@123')
  console.log('Đã lấy token xác thực thành công.')

  const browser = await puppeteer.launch({
    executablePath: CHROME_PATH,
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,1000']
  })

  const auditResults = {
    timestamp: new Date().toISOString(),
    environment: {
      node: process.version,
      platform: process.platform,
      chrome: '155.0.8059.86',
      dotnet: '9.0.308 (.NET 8 runtime)',
      frontendUrl: BASE_URL,
      backendUrl: API_URL
    },
    scores: {},
    metrics: {},
    audits: {}
  }

  try {
    // 1. Lighthouse User Desktop
    console.log('--- 2. CHẠY LIGHTHOUSE: USER DESKTOP ---')
    const userDesktop = await runLighthouseAudit(browser, userTokens, false, 'lighthouse-user-desktop')
    const udLhr = userDesktop.lhr
    auditResults.scores.userDesktop = {
      seo: Math.round(udLhr.categories.seo?.score * 100),
      accessibility: Math.round(udLhr.categories.accessibility?.score * 100),
      bestPractices: Math.round(udLhr.categories['best-practices']?.score * 100),
      performance: Math.round(udLhr.categories.performance?.score * 100)
    }
    auditResults.metrics.userDesktop = {
      fcp: udLhr.audits['first-contentful-paint']?.displayValue,
      lcp: udLhr.audits['largest-contentful-paint']?.displayValue,
      cls: udLhr.audits['cumulative-layout-shift']?.displayValue,
      tbt: udLhr.audits['total-blocking-time']?.displayValue,
      si: udLhr.audits['speed-index']?.displayValue
    }
    console.log('User Desktop Scores:', auditResults.scores.userDesktop)
    console.log('User Desktop Metrics:', auditResults.metrics.userDesktop)

    // 2. Lighthouse User Mobile
    console.log('--- 3. CHẠY LIGHTHOUSE: USER MOBILE ---')
    const userMobile = await runLighthouseAudit(browser, userTokens, true, 'lighthouse-user-mobile')
    const umLhr = userMobile.lhr
    auditResults.scores.userMobile = {
      seo: Math.round(umLhr.categories.seo?.score * 100),
      accessibility: Math.round(umLhr.categories.accessibility?.score * 100),
      bestPractices: Math.round(umLhr.categories['best-practices']?.score * 100),
      performance: Math.round(umLhr.categories.performance?.score * 100)
    }
    auditResults.metrics.userMobile = {
      fcp: umLhr.audits['first-contentful-paint']?.displayValue,
      lcp: umLhr.audits['largest-contentful-paint']?.displayValue,
      cls: umLhr.audits['cumulative-layout-shift']?.displayValue,
      tbt: umLhr.audits['total-blocking-time']?.displayValue,
      si: umLhr.audits['speed-index']?.displayValue
    }
    console.log('User Mobile Scores:', auditResults.scores.userMobile)
    console.log('User Mobile Metrics:', auditResults.metrics.userMobile)

    // 3. Lighthouse Admin Desktop
    console.log('--- 4. CHẠY LIGHTHOUSE: ADMIN DESKTOP ---')
    const adminDesktop = await runLighthouseAudit(browser, adminTokens, false, 'lighthouse-admin-desktop')
    const adLhr = adminDesktop.lhr
    auditResults.scores.adminDesktop = {
      seo: Math.round(adLhr.categories.seo?.score * 100),
      accessibility: Math.round(adLhr.categories.accessibility?.score * 100),
      bestPractices: Math.round(adLhr.categories['best-practices']?.score * 100),
      performance: Math.round(adLhr.categories.performance?.score * 100)
    }
    auditResults.metrics.adminDesktop = {
      fcp: adLhr.audits['first-contentful-paint']?.displayValue,
      lcp: adLhr.audits['largest-contentful-paint']?.displayValue,
      cls: adLhr.audits['cumulative-layout-shift']?.displayValue,
      tbt: adLhr.audits['total-blocking-time']?.displayValue,
      si: adLhr.audits['speed-index']?.displayValue
    }
    console.log('Admin Desktop Scores:', auditResults.scores.adminDesktop)

    // Save failed audits
    const getFailedAudits = (lhr) => {
      return Object.entries(lhr.audits)
        .filter(([_, a]) => a.score !== null && a.score < 1 && a.scoreDisplayMode !== 'notApplicable' && a.scoreDisplayMode !== 'informative')
        .map(([id, a]) => ({ id, title: a.title, score: a.score, displayValue: a.displayValue, description: a.description }))
    }
    auditResults.audits.failedUserDesktop = getFailedAudits(udLhr)
    auditResults.audits.failedUserMobile = getFailedAudits(umLhr)
    auditResults.audits.failedAdminDesktop = getFailedAudits(adLhr)

    fs.writeFileSync(path.join(OUTPUT_DIR, 'lighthouse-summary.json'), JSON.stringify(auditResults, null, 2), 'utf8')
    console.log('Đã lưu lighthouse-summary.json')

  } finally {
    await browser.close()
  }
}

main().catch(err => {
  console.error(err)
  process.exit(1)
})
