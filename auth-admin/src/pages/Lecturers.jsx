import React, { useEffect, useState } from "react";
import axios from "axios";
import { API_CONFIG, buildUrl } from "../lib/apiConfig.js";

const API = buildUrl(API_CONFIG.ENDPOINTS.DEPARTMENTS.BASE);
const SUBJECT_API = buildUrl(API_CONFIG.ENDPOINTS.SUBJECTS);

export default function LecturerAssignments() {
  const [data, setData] = useState([]);
  const [form, setForm] = useState({ lecturerId: "", majors: {} });
  const [editMode, setEditMode] = useState(false);
  const [loading, setLoading] = useState(false);
  const [showPopup, setShowPopup] = useState(false);
  const [subjectList, setSubjectList] = useState([]);
  const [newMajor, setNewMajor] = useState("");
  const [search, setSearch] = useState("");
  const allMajors = [
    "AI",
    "BA",
    "EC",
    "EL",
    "FN",
    "HM",
    "GD",
    "IB",
    "IA",
    "KR",
    "JL",
    "SE",
    "MC",
    "TM",
  ];
  const user = React.useMemo(() => {
    try {
      return JSON.parse(localStorage.getItem("aa_user") || "{}");
    } catch {
      return {};
    }
  }, []);

  const [lecturerList, setLecturerList] = useState([]);
  const hasDepartment = !!user.department;

  const fetchLecturers = async () => {
    const res = await axios.get(buildUrl(API_CONFIG.ENDPOINTS.LECTURERS), {
      headers: {
        Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
      },
    });
    setLecturerList(res.data || []);
  };

  // Lấy danh sách phân công
  const fetchData = async () => {
    setLoading(true);
    const res = await axios.get(API, {
      headers: {
        Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
      },
    });
    setData(res.data);
    setLoading(false);
  };

  // Lấy danh sách môn học
  const fetchSubjects = async () => {
    const res = await axios.get(SUBJECT_API, {
      headers: {
        Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
      },
    });
    setSubjectList(res.data || []);
  };

  useEffect(() => {
    fetchData();
    fetchSubjects();
    fetchLecturers();
  }, []);

  // Xử lý thay đổi form
  const handleFormChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  // Thêm subject cho major
  const addSubject = (major) => {
    setForm((prev) => ({
      ...prev,
      majors: {
        ...prev.majors,
        [major]: [
          ...(prev.majors[major] || []),
          {
            subjectCode: "",
            subjectName: "",
            term: "",
            numberOfClasses: "",
            totalSlots: "",
          },
        ],
      },
    }));
  };

  // Thay đổi subject trong major
  const handleSubjectChange = (major, idx, field, value) => {
    setForm((prev) => {
      const subjects = [...(prev.majors[major] || [])];
      if (field === "subjectCode") {
        const found = subjectList.find((s) => s.subjectCode === value);
        subjects[idx].subjectCode = value;
        subjects[idx].subjectName = found
          ? found.subjectNameEnglish || found.subjectNameVietnamese
          : "";
      } else {
        subjects[idx][field] = value;
      }
      return {
        ...prev,
        majors: { ...prev.majors, [major]: subjects },
      };
    });
  };

  // Thêm major mới (input)
  const handleAddMajor = () => {
    // Nếu user có department, chỉ cho thêm đúng chuyên ngành đó
    if (hasDepartment) {
      if (!form.majors[user.department]) {
        setForm((prev) => ({
          ...prev,
          majors: { ...prev.majors, [user.department]: [] },
        }));
      }
    } else {
      if (newMajor && !form.majors[newMajor]) {
        setForm((prev) => ({
          ...prev,
          majors: { ...prev.majors, [newMajor]: [] },
        }));
        setNewMajor("");
      }
    }
  };

  // Xóa major
  const removeMajor = (major) => {
    setForm((prev) => {
      const majors = { ...prev.majors };
      delete majors[major];
      return { ...prev, majors };
    });
  };

  // Xóa subject khỏi major
  const removeSubject = (major, idx) => {
    setForm((prev) => {
      const subjects = [...(prev.majors[major] || [])];
      subjects.splice(idx, 1);
      return {
        ...prev,
        majors: { ...prev.majors, [major]: subjects },
      };
    });
  };

  // Submit tạo mới hoặc cập nhật
  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      let submitForm = { ...form };
      // Nếu user có department, chỉ gửi majors với chuyên ngành đó
      if (hasDepartment) {
        submitForm.majors = {
          [user.department]: form.majors[user.department] || [],
        };
      }
      if (editMode) {
        await axios.put(API, submitForm, {
          headers: {
            Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
          },
        });
      } else {
        await axios.post(API, submitForm, {
          headers: {
            Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
          },
        });
      }
      setForm({ lecturerId: "", majors: {} });
      setEditMode(false);
      setShowPopup(false);
      fetchData();
    } catch (err) {
      alert("Lỗi: " + (err.response?.data?.message || err.message));
    }
    setLoading(false);
  };

  // Xóa toàn bộ phân công của một giảng viên
  const handleDelete = async (lecturerId) => {
    if (!window.confirm("Xóa toàn bộ phân công của giảng viên này?")) return;
    setLoading(true);
    await axios.delete(`${API}/${lecturerId}`, {
      headers: {
        Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
      },
    });
    fetchData();
    setLoading(false);
  };

  const handleDownload = async () => {
    try {
      const res = await axios.get(buildUrl(API_CONFIG.ENDPOINTS.DEPARTMENTS.EXPORT), {
        headers: {
          Authorization: "Bearer " + localStorage.getItem("aa_accessToken"),
        },
        responseType: "blob",
      });

      const now = new Date();
      const pad = (n) => String(n).padStart(2, "0");
      const timestamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(
        now.getDate()
      )}_${pad(now.getHours())}-${pad(now.getMinutes())}-${pad(
        now.getSeconds()
      )}`;

      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", `lecturers_subject_${timestamp}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      alert("Lỗi tải file: " + (err.response?.data?.message || err.message));
    }
  };

  // Chọn để sửa
  const handleEdit = (lecturer) => {
    setForm({
      lecturerId: lecturer.lecturerId,
      majors: lecturer.majors || {},
    });
    setEditMode(true);
    setShowPopup(true);
  };

  // Đóng popup
  const handleClosePopup = () => {
    setForm({ lecturerId: "", majors: {} });
    setEditMode(false);
    setShowPopup(false);
    setNewMajor("");
  };

  return (
    <div>
      <div
        style={{
          display: "flex",
          flexDirection: "column", // sắp xếp theo cột
          alignItems: "center", // căn giữa theo chiều ngang
          // marginBottom: 16,
        }}
      >
        <h2
          style={{
            margin: 0,
            fontSize: 34,
            fontWeight: "bold",
            textAlign: "center",
          }}
        >
          Quản lý phân công giảng viên
        </h2>
        <div style={{ marginTop: 12, alignSelf: "flex-end" }}>
          <button
            style={{
              marginTop: 12, // tạo khoảng cách với h2
              marginBottom: 8,
              marginRight: 10,
              background: "#1976d2",
              color: "#fff",
              border: "none",
              padding: "10px 24px",
              borderRadius: 6,
              fontWeight: 600,
              fontSize: 16,
              cursor: "pointer",
              boxShadow: "0 2px 8px rgba(25, 118, 210, 0.15)",
              transition: "background 0.2s",
              alignSelf: "flex-end", // nút nằm bên phải
            }}
            onMouseOver={(e) => (e.currentTarget.style.background = "#1565c0")}
            onMouseOut={(e) => (e.currentTarget.style.background = "#1976d2")}
            onClick={handleDownload}
          >
            ⬇ Tải xuống
          </button>

          <button
            style={{
              marginTop: 12, // tạo khoảng cách với h2
              marginBottom: 8,
              background: "#1976d2",
              color: "#fff",
              border: "none",
              padding: "10px 24px",
              borderRadius: 6,
              fontWeight: 600,
              fontSize: 16,
              cursor: "pointer",
              boxShadow: "0 2px 8px rgba(25, 118, 210, 0.15)",
              transition: "background 0.2s",
              alignSelf: "flex-end", // nút nằm bên phải
            }}
            onMouseOver={(e) => (e.currentTarget.style.background = "#1565c0")}
            onMouseOut={(e) => (e.currentTarget.style.background = "#1976d2")}
            onClick={() => {
              setForm({ lecturerId: "", majors: {} });
              setEditMode(false);
              setShowPopup(true);
            }}
          >
            ➕ Thêm mới
          </button>
        </div>
      </div>

      <div
        style={{
          display: "flex",
          justifyContent: "flex-start",
          marginBottom: 20,
        }}
      >
        <div
          style={{
            position: "relative",
            width: 360,
          }}
        >
          <span
            style={{
              position: "absolute",
              left: 12,
              top: "50%",
              transform: "translateY(-50%)",
              color: "#888",
              fontSize: 18,
              pointerEvents: "none",
            }}
          >
            🔍
          </span>
          <input
            type="text"
            placeholder="Tìm theo mã GV hoặc tài khoản..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{
              padding: "10px 14px 10px 38px",
              borderRadius: 24,
              border: "1.5px solid #ccc",
              minWidth: 320,
              fontSize: 16,
              background: "#f7f9fc",
              boxShadow: "0 2px 8px rgba(25, 118, 210, 0.07)",
              outline: "none",
              transition: "border 0.2s",
            }}
            onFocus={(e) => (e.target.style.border = "1.5px solid #1976d2")}
            onBlur={(e) => (e.target.style.border = "1.5px solid #ccc")}
          />
        </div>
      </div>

      {loading && <div>Đang tải...</div>}

      {/* Popup Form */}
      {showPopup && (
        <div
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            width: "100vw",
            height: "100vh",
            background: "rgba(0,0,0,0.25)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 1000,
          }}
        >
          <div
            style={{
              background: "#fff",
              padding: 36,
              borderRadius: 12,
              minWidth: 600,
              maxWidth: "90vw",
              boxShadow: "0 4px 24px rgba(0,0,0,0.18)",
              position: "relative",
              maxHeight: "90vh",
              overflowY: "auto",
            }}
          >
            <button
              onClick={handleClosePopup}
              style={{
                position: "absolute",
                top: 16,
                right: 16,
                background: "#e53935",
                color: "#fff",
                border: "none",
                fontSize: 22,
                borderRadius: "50%",
                width: 36,
                height: 36,
                cursor: "pointer",
                boxShadow: "0 2px 8px rgba(229,57,53,0.12)",
                transition: "background 0.2s",
              }}
              title="Đóng"
              onMouseOver={(e) =>
                (e.currentTarget.style.background = "#b71c1c")
              }
              onMouseOut={(e) => (e.currentTarget.style.background = "#e53935")}
            >
              ×
            </button>
            <form onSubmit={handleSubmit}>
              <div style={{ marginBottom: 18 }}>
                <label
                  style={{ fontWeight: 500, marginBottom: 4, display: "block" }}
                >
                  Giảng viên
                </label>
                {editMode ? (
                  <input
                    name="lecturerId"
                    value={form.lecturerId}
                    disabled
                    style={{
                      marginBottom: 8,
                      padding: "8px 12px",
                      borderRadius: 4,
                      border: "1px solid #ccc",
                      background: "#f5f5f5",
                      width: "100%",
                    }}
                  />
                ) : (
                  <select
                    name="lecturerId"
                    value={form.lecturerId}
                    onChange={handleFormChange}
                    required
                    style={{
                      minWidth: 220,
                      marginBottom: 8,
                      padding: "8px 12px",
                      borderRadius: 4,
                      border: "1px solid #ccc",
                      width: "100%",
                    }}
                  >
                    <option value="">-- Chọn giảng viên --</option>
                    {lecturerList.map((l) => (
                      <option key={l.lecturerId} value={l.lecturerId}>
                        {l.lecturerId} - {l.lecturerName}
                      </option>
                    ))}
                  </select>
                )}
              </div>

              {!hasDepartment && (
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 12,
                    marginBottom: 12,
                  }}
                >
                  <label style={{ fontWeight: 500, marginRight: 8 }}>
                    Chuyên ngành
                  </label>
                  <select
                    value={newMajor}
                    onChange={(e) => setNewMajor(e.target.value)}
                    style={{
                      minWidth: 180,
                      padding: "8px 12px",
                      borderRadius: 4,
                      border: "1px solid #ccc",
                      background: "#fff",
                      fontSize: 16,
                    }}
                  >
                    <option value="">-- Chọn chuyên ngành --</option>
                    {allMajors
                      .filter((m) => !form.majors[m])
                      .map((m) => (
                        <option key={m} value={m}>
                          {m}
                        </option>
                      ))}
                  </select>
                  <button
                    type="button"
                    onClick={handleAddMajor}
                    disabled={!newMajor}
                    style={{
                      background: "#43a047",
                      color: "#fff",
                      border: "none",
                      padding: "8px 18px",
                      borderRadius: 6,
                      fontWeight: 500,
                      cursor: newMajor ? "pointer" : "not-allowed",
                      opacity: newMajor ? 1 : 0.6,
                      transition: "background 0.2s",
                    }}
                    onMouseOver={(e) => {
                      if (newMajor)
                        e.currentTarget.style.background = "#388e3c";
                    }}
                    onMouseOut={(e) => {
                      if (newMajor)
                        e.currentTarget.style.background = "#43a047";
                    }}
                  >
                    + Thêm chuyên ngành
                  </button>
                </div>
              )}

              {hasDepartment && (
                <div style={{ marginBottom: 12 }}>
                  <label style={{ fontWeight: 500, marginRight: 8 }}>
                    Chuyên ngành
                  </label>
                  <span
                    style={{
                      fontWeight: 600,
                      padding: "8px 18px",
                      borderRadius: 6,
                      background: "#f5f5f5",
                      border: "1px solid #ccc",
                      marginLeft: 8,
                      display: "inline-block",
                    }}
                  >
                    {user.department}
                  </span>
                  <button
                    type="button"
                    onClick={handleAddMajor}
                    disabled={!!form.majors[user.department]}
                    style={{
                      background: "#43a047",
                      color: "#fff",
                      border: "none",
                      padding: "8px 18px",
                      borderRadius: 6,
                      fontWeight: 500,
                      cursor: !form.majors[user.department]
                        ? "pointer"
                        : "not-allowed",
                      opacity: !form.majors[user.department] ? 1 : 0.6,
                      marginLeft: 12,
                      transition: "background 0.2s",
                    }}
                    onMouseOver={(e) => {
                      if (!form.majors[user.department])
                        e.currentTarget.style.background = "#388e3c";
                    }}
                    onMouseOut={(e) => {
                      if (!form.majors[user.department])
                        e.currentTarget.style.background = "#43a047";
                    }}
                  >
                    + Thêm chuyên ngành
                  </button>
                </div>
              )}

              {Object.entries(form.majors).map(([major, subjects]) => (
                <div
                  key={major}
                  style={{
                    marginTop: 12,
                    border: "1px solid #e0e0e0",
                    padding: 12,
                    borderRadius: 6,
                    background: "#fafafa",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      marginBottom: 6,
                      justifyContent: "space-between",
                    }}
                  >
                    <b style={{ fontSize: 16 }}>{major}</b>
                    <div style={{ display: "flex", gap: 8 }}>
                      <button
                        type="button"
                        onClick={() => addSubject(major)}
                        style={{
                          background: "#1976d2",
                          color: "#fff",
                          border: "none",
                          borderRadius: 4,
                          padding: "4px 12px",
                          fontWeight: 500,
                          cursor: "pointer",
                          transition: "background 0.2s",
                        }}
                        onMouseOver={(e) =>
                          (e.currentTarget.style.background = "#1565c0")
                        }
                        onMouseOut={(e) =>
                          (e.currentTarget.style.background = "#1976d2")
                        }
                      >
                        + Thêm môn
                      </button>
                      <button
                        type="button"
                        onClick={() => removeMajor(major)}
                        style={{
                          color: "#fff",
                          background: "#e53935",
                          border: "none",
                          borderRadius: 4,
                          padding: "4px 12px",
                          fontWeight: 500,
                          cursor: "pointer",
                          transition: "background 0.2s",
                        }}
                        onMouseOver={(e) =>
                          (e.currentTarget.style.background = "#b71c1c")
                        }
                        onMouseOut={(e) =>
                          (e.currentTarget.style.background = "#e53935")
                        }
                      >
                        Xóa chuyên ngành
                      </button>
                    </div>
                  </div>
                  {(subjects || []).map((subject, idx) => (
                    <div
                      key={idx}
                      style={{
                        marginLeft: 16,
                        marginTop: 6,
                        display: "flex",
                        alignItems: "center",
                        gap: 8,
                      }}
                    >
                      <label style={{ minWidth: 80, fontWeight: 500 }}>
                        Mã môn
                      </label>
                      <select
                        value={subject.subjectCode}
                        onChange={(e) =>
                          handleSubjectChange(
                            major,
                            idx,
                            "subjectCode",
                            e.target.value
                          )
                        }
                        required
                        style={{
                          width: 140,
                          padding: "6px 10px",
                          borderRadius: 4,
                          border: "1px solid #bbb",
                        }}
                      >
                        <option value="">-- Mã môn --</option>
                        {subjectList.map((s) => (
                          <option key={s.subjectCode} value={s.subjectCode}>
                            {s.subjectCode}
                          </option>
                        ))}
                      </select>
                      <label style={{ minWidth: 80, fontWeight: 500 }}>
                        Tên môn
                      </label>
                      <input
                        placeholder="Tên môn"
                        value={subject.subjectName}
                        readOnly
                        style={{
                          width: 160,
                          background: "#f5f5f5",
                          padding: "6px 10px",
                          borderRadius: 4,
                          border: "1px solid #bbb",
                        }}
                      />
                      <label style={{ minWidth: 40, fontWeight: 500 }}>
                        Kỳ
                      </label>
                      <input
                        placeholder="Kỳ"
                        value={subject.term}
                        required
                        onChange={(e) =>
                          handleSubjectChange(
                            major,
                            idx,
                            "term",
                            e.target.value
                          )
                        }
                        style={{
                          width: 50,
                          padding: "6px 10px",
                          borderRadius: 4,
                          border: "1px solid #bbb",
                        }}
                      />
                      <label style={{ minWidth: 60, fontWeight: 500 }}>
                        Số lớp
                      </label>
                      <input
                        placeholder="Số lớp"
                        value={subject.numberOfClasses}
                        required
                        onChange={(e) =>
                          handleSubjectChange(
                            major,
                            idx,
                            "numberOfClasses",
                            e.target.value
                          )
                        }
                        style={{
                          width: 70,
                          padding: "6px 10px",
                          borderRadius: 4,
                          border: "1px solid #bbb",
                        }}
                      />
                      <label style={{ minWidth: 80, fontWeight: 500 }}>
                        Tổng slot
                      </label>
                      <input
                        placeholder="Tổng slot"
                        value={subject.totalSlots}
                        required
                        onChange={(e) =>
                          handleSubjectChange(
                            major,
                            idx,
                            "totalSlots",
                            e.target.value
                          )
                        }
                        style={{
                          width: 80,
                          padding: "6px 10px",
                          borderRadius: 4,
                          border: "1px solid #bbb",
                        }}
                      />
                      <button
                        type="button"
                        onClick={() => removeSubject(major, idx)}
                        style={{
                          color: "#fff",
                          background: "#e53935",
                          border: "none",
                          borderRadius: 4,
                          padding: "4px 12px",
                          fontWeight: 500,
                          cursor: "pointer",
                          marginLeft: 8,
                          transition: "background 0.2s",
                        }}
                        onMouseOver={(e) =>
                          (e.currentTarget.style.background = "#b71c1c")
                        }
                        onMouseOut={(e) =>
                          (e.currentTarget.style.background = "#e53935")
                        }
                      >
                        🗑️
                      </button>
                    </div>
                  ))}
                </div>
              ))}

              <div
                style={{
                  marginTop: 24,
                  display: "flex",
                  gap: 12,
                  justifyContent: "flex-end",
                }}
              >
                <button
                  type="submit"
                  disabled={loading}
                  style={{
                    background: "#1976d2",
                    color: "#fff",
                    border: "none",
                    padding: "10px 28px",
                    borderRadius: 6,
                    fontWeight: 600,
                    fontSize: 16,
                    cursor: "pointer",
                    boxShadow: "0 2px 8px rgba(25, 118, 210, 0.12)",
                    transition: "background 0.2s",
                  }}
                  onMouseOver={(e) =>
                    (e.currentTarget.style.background = "#1565c0")
                  }
                  onMouseOut={(e) =>
                    (e.currentTarget.style.background = "#1976d2")
                  }
                >
                  {editMode ? "Cập nhật" : "Tạo mới"}
                </button>
                <button
                  type="button"
                  onClick={handleClosePopup}
                  style={{
                    background: "#eee",
                    color: "#333",
                    border: "none",
                    padding: "10px 28px",
                    borderRadius: 6,
                    fontWeight: 600,
                    fontSize: 16,
                    cursor: "pointer",
                    boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
                    transition: "background 0.2s",
                  }}
                  onMouseOver={(e) =>
                    (e.currentTarget.style.background = "#ccc")
                  }
                  onMouseOut={(e) =>
                    (e.currentTarget.style.background = "#eee")
                  }
                >
                  Hủy
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* <h3>Danh sách phân công</h3> */}
      <div style={{ overflowX: "auto" }}>
        <table
          style={{
            borderCollapse: "collapse",
            width: "100%",
            background: "#fff",
            boxShadow: "0 2px 8px rgba(0,0,0,0.05)",
            borderRadius: 8,
            overflow: "hidden",
          }}
        >
          <thead style={{ background: "#1976d2", color: "#fff" }}>
            <tr>
              <th style={{ padding: 8 }}>Mã GV</th>
              <th style={{ padding: 8 }}>Tài khoản</th>
              <th style={{ padding: 8 }}>Tên</th>
              <th style={{ padding: 8 }}>Khoa</th>
              <th style={{ padding: 8 }}>Chuyên ngành & Môn học</th>
              <th style={{ padding: 8 }}>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {data
              .filter(
                (l) =>
                  l.lecturerId?.toLowerCase().includes(search.toLowerCase()) ||
                  l.lecturerAccount
                    ?.toLowerCase()
                    .includes(search.toLowerCase())
              )
              .map((lecturer, idx) => (
                <tr
                  key={lecturer.lecturerId}
                  style={{
                    background: idx % 2 === 0 ? "#f9f9f9" : "#fff",
                    borderBottom: "1px solid #eee",
                  }}
                >
                  <td style={{ padding: 8 }}>{lecturer.lecturerId}</td>
                  <td style={{ padding: 8 }}>{lecturer.lecturerAccount}</td>
                  <td style={{ padding: 8 }}>{lecturer.lecturerName}</td>
                  <td style={{ padding: 8 }}>{lecturer.department}</td>
                  <td style={{ padding: 8 }}>
                    {Object.entries(lecturer.majors || {}).map(
                      ([major, subjects]) => (
                        <div key={major} style={{ marginBottom: 4 }}>
                          <b>{major}:</b>
                          <ul style={{ margin: 0, paddingLeft: 16 }}>
                            {subjects.map((s, i) => (
                              <li key={i}>
                                {s.subjectCode} - {s.subjectName}
                              </li>
                            ))}
                          </ul>
                        </div>
                      )
                    )}
                  </td>
                  <td style={{ padding: 8 }}>
                    <button
                      onClick={() => handleEdit(lecturer)}
                      style={{
                        background: "#ffa726",
                        color: "#fff",
                        border: "none",
                        borderRadius: 4,
                        padding: "6px 16px",
                        marginRight: 8,
                        fontWeight: 500,
                        cursor: "pointer",
                        transition: "background 0.2s",
                      }}
                      onMouseOver={(e) =>
                        (e.currentTarget.style.background = "#fb8c00")
                      }
                      onMouseOut={(e) =>
                        (e.currentTarget.style.background = "#ffa726")
                      }
                    >
                      ✏️
                    </button>
                    <button
                      onClick={() => handleDelete(lecturer.lecturerId)}
                      style={{
                        background: "#e53935",
                        color: "#fff",
                        border: "none",
                        borderRadius: 4,
                        padding: "6px 16px",
                        fontWeight: 500,
                        cursor: "pointer",
                        transition: "background 0.2s",
                      }}
                      onMouseOver={(e) =>
                        (e.currentTarget.style.background = "#b71c1c")
                      }
                      onMouseOut={(e) =>
                        (e.currentTarget.style.background = "#e53935")
                      }
                    >
                      🗑️
                    </button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
