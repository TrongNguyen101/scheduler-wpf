const express = require("express");
const { Lecturer } = require("../models");

const router = express.Router();

router.get("/", async (req, res) => {
  const lecturers = await Lecturer.find().lean();
  res.json(lecturers);
});

module.exports = router;
