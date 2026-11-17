import textwrap
from pathlib import Path
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = Path(__file__).parent
COMPANY = "Maple Mutual Insurance (sample, fictional)"

CLAIMS = [
    dict(file="claim_01_clean_form.pdf", style="form", name="Sarah Johnson", policy="PM-482913",
         date="September 14, 2026", amount="$3,450.00",
         desc="Rear-ended at a traffic light. Bumper and tail light damaged."),
    dict(file="claim_02_clean_letter.pdf", style="letter", name="Daniel Okafor", policy="PM-305517",
         date="March 3, 2026", amount="$1,280.50",
         desc="A burst pipe caused water damage to my kitchen ceiling."),
    dict(file="claim_03_missing_amount.pdf", style="form", name="Priya Nair", policy="PM-771204",
         date="July 22, 2026", amount=None,
         desc="Cracked windshield from a stone on the highway."),
    dict(file="claim_04_missing_policy.pdf", style="form", name="Michael Chen", policy=None,
         date="May 9, 2026", amount="$865.00",
         desc="Side mirror broken in a parking lot."),
    dict(file="claim_05_ambiguous_date.pdf", style="form", name="Emily Tremblay", policy="PM-640982",
         date="03/04/2026", amount="$2,150.00",
         desc="Fence damaged by a fallen tree during a storm."),
    dict(file="claim_06_currency_text.pdf", style="letter", name="Omar Haddad", policy="PM-118830",
         date="October 2, 2026", amount="CAD 12,975.00",
         desc="Hail damaged the roof and two skylights."),
    dict(file="claim_07_future_date.pdf", style="form", name="Laura Singh", policy="PM-452291",
         date="December 15, 2026", amount="$540.00",
         desc="Bicycle stolen from a locked garage."),
    dict(file="claim_08_bad_policy_format.pdf", style="form", name="James Walker", policy="POL-9981",
         date="February 11, 2026", amount="$4,300.00",
         desc="Kitchen fire caused smoke damage to cabinets."),
]


def pdf_form(d):
    c = canvas.Canvas(str(OUT / d["file"]), pagesize=letter)
    _, h = letter
    c.setFont("Helvetica-Bold", 18)
    c.drawString(72, h - 72, "INSURANCE CLAIM FORM")
    c.setFont("Helvetica", 11)
    c.drawString(72, h - 92, COMPANY)
    y = h - 140
    rows = [("Claimant Name", d["name"]), ("Policy Number", d["policy"]),
            ("Date of Incident", d["date"]), ("Amount Claimed", d["amount"])]
    for label, val in rows:
        c.setFont("Helvetica-Bold", 11)
        c.drawString(72, y, label + ":")
        c.setFont("Helvetica", 11)
        c.drawString(220, y, val or "____________________")
        y -= 28
    c.setFont("Helvetica-Bold", 11)
    c.drawString(72, y, "Description of Incident:")
    y -= 20
    c.setFont("Helvetica", 11)
    for line in textwrap.wrap(d["desc"], 80):
        c.drawString(72, y, line)
        y -= 16
    c.save()


def pdf_letter(d):
    c = canvas.Canvas(str(OUT / d["file"]), pagesize=letter)
    _, h = letter
    c.setFont("Helvetica-Bold", 14)
    c.drawString(72, h - 72, "Claims Department")
    c.setFont("Helvetica", 11)
    c.drawString(72, h - 90, COMPANY)
    body = "To whom it may concern, I am writing to submit a claim. My name is " + d["name"]
    body += f" and my policy number is {d['policy']}." if d["policy"] else "."
    body += f" The incident happened on {d['date']}."
    if d["amount"]:
        body += f" I am requesting {d['amount']} in compensation."
    body += f" {d['desc']} Sincerely, {d['name']}"
    y = h - 140
    for line in textwrap.wrap(body, 85):
        c.drawString(72, y, line)
        y -= 16
    c.save()


def blurry_png():
    img = Image.new("RGB", (1000, 700), "white")
    draw = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 30)
    except OSError:
        font = ImageFont.load_default()
    lines = ["INSURANCE CLAIM FORM", "",
             "Claimant Name: Hannah Brooks", "Policy Number: PM-903377",
             "Date of Incident: August 18, 2026", "Amount Claimed: $2,980.00",
             "Description: Basement flooded after heavy rain."]
    y = 80
    for line in lines:
        draw.text((60, y), line, fill="black", font=font)
        y += 60
    img.filter(ImageFilter.GaussianBlur(9)).save(OUT / "claim_09_blurry.png")


def not_a_claim():
    c = canvas.Canvas(str(OUT / "claim_10_not_a_claim.pdf"), pagesize=letter)
    _, h = letter
    c.setFont("Helvetica-Bold", 16)
    c.drawString(72, h - 72, "GREEN GROCER RECEIPT")
    c.setFont("Helvetica", 11)
    y = h - 110
    for line in ["Apples 2 kg ........ $6.40", "Bread ............... $3.50",
                 "Milk 2 L ............ $4.99", "TOTAL ............... $14.89"]:
        c.drawString(72, y, line)
        y -= 20
    c.save()


for claim in CLAIMS:
    (pdf_form if claim["style"] == "form" else pdf_letter)(claim)
blurry_png()
not_a_claim()
print("Created 10 sample files in", OUT)