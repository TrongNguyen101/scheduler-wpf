const express = require("express");
const { Subject } = require("../models");

const router = express.Router();

router.get("/", async (req, res) => {
  const subjects = await Subject.find().lean();
  res.json(subjects);
});

module.exports = router;
